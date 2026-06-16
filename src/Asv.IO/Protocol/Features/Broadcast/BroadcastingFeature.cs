using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;

namespace Asv.IO;

public class BroadcastingFeature<TMessage> : IProtocolFeature
{
    public const string FeatureId = $"Broadcasting {nameof(TMessage)}";
    private readonly ConditionalWeakTable<
        IProtocolConnection,
        BroadcastLoopProtectionState
    > _loopProtectionStates = new();
    private readonly BroadcastingFeatureOptions _options;

    public BroadcastingFeature()
        : this(BroadcastingFeatureOptions.Default) { }

    public BroadcastingFeature(bool isLoopProtectionEnabled)
        : this(new BroadcastingFeatureOptions { IsLoopProtectionEnabled = isLoopProtectionEnabled })
    { }

    public BroadcastingFeature(BroadcastingFeatureOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.LoopProtectionWindow <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Loop protection window must be greater than zero."
            );
        }

        if (options.MaxLoopProtectionCacheSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Loop protection cache size must be greater than zero."
            );
        }

        _options = options;
    }

    public string Name => $"Message Broadcasting {nameof(TMessage)}";
    public string Description =>
        "Allows retransmission of incoming messages to all other connection endpoints.";
    public string Id => FeatureId;
    public int Priority => 0;

    public async ValueTask<IProtocolMessage?> ProcessRx(
        IProtocolMessage message,
        IProtocolConnection connection,
        CancellationToken cancel
    )
    {
        if (message is not TMessage)
        {
            return message;
        }

        if (connection is IProtocolEndpoint endpoint)
        {
            // mark message with connection id
            message.MarkBroadcast(connection);
        }

        if (connection is IProtocolRouter router)
        {
            // all received messages broadcast to all other connections
            if (ShouldBroadcast(message, router))
            {
                await router.Send(message, cancel);
            }
        }
        return message;
    }

    public ValueTask<IProtocolMessage?> ProcessTx(
        IProtocolMessage message,
        IProtocolConnection connection,
        CancellationToken cancel
    )
    {
        if (message is not TMessage)
        {
            return ValueTask.FromResult<IProtocolMessage?>(message);
        }

        if (connection is IProtocolRouter router)
        {
            RememberBroadcast(message, router);
        }

        if (connection is IProtocolEndpoint endpoint && message is TMessage)
        {
            // check if message was received by that connection => skip it
            return message.CheckBroadcast(endpoint)
                ? default
                : ValueTask.FromResult<IProtocolMessage?>(message);
        }
        return ValueTask.FromResult<IProtocolMessage?>(message);
    }

    public void Register(IProtocolConnection connection)
    {
        if (connection is IProtocolRouter && _options.IsLoopProtectionEnabled)
        {
            _loopProtectionStates.GetValue(
                connection,
                _ => new BroadcastLoopProtectionState(_options)
            );
        }
    }

    public void Unregister(IProtocolConnection connection)
    {
        _loopProtectionStates.Remove(connection);
    }

    private bool ShouldBroadcast(IProtocolMessage message, IProtocolRouter router)
    {
        if (_options.IsLoopProtectionEnabled == false)
        {
            return true;
        }

        return GetState(router).TryRemember(CalculateCrc32(message));
    }

    private void RememberBroadcast(IProtocolMessage message, IProtocolRouter router)
    {
        if (_options.IsLoopProtectionEnabled == false)
        {
            return;
        }

        GetState(router).Remember(CalculateCrc32(message));
    }

    private BroadcastLoopProtectionState GetState(IProtocolConnection connection)
    {
        return _loopProtectionStates.GetValue(
            connection,
            _ => new BroadcastLoopProtectionState(_options)
        );
    }

    private static uint CalculateCrc32(IProtocolMessage message)
    {
        var size = message.GetByteSize();
        var buffer = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            var span = buffer.AsSpan(0, size);
            var remaining = span;
            message.Serialize(ref remaining);
            remaining.Clear();
            return Crc32Mavlink.Accumulate(span);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private sealed class BroadcastLoopProtectionState
    {
        private readonly long _windowTimestampTicks;
        private readonly Dictionary<uint, long> _seen = [];
        private readonly object _sync = new();
        private readonly int _maxCacheSize;

        public BroadcastLoopProtectionState(BroadcastingFeatureOptions options)
        {
            _windowTimestampTicks = Math.Max(
                1L,
                (long)(options.LoopProtectionWindow.TotalSeconds * Stopwatch.Frequency)
            );
            _maxCacheSize = options.MaxLoopProtectionCacheSize;
        }

        public bool TryRemember(uint crc32)
        {
            var timestamp = Stopwatch.GetTimestamp();
            lock (_sync)
            {
                CleanupExpired(timestamp);
                if (_seen.ContainsKey(crc32))
                {
                    return false;
                }

                RememberCore(crc32, timestamp);
                return true;
            }
        }

        public void Remember(uint crc32)
        {
            var timestamp = Stopwatch.GetTimestamp();
            lock (_sync)
            {
                CleanupExpired(timestamp);
                RememberCore(crc32, timestamp);
            }
        }

        private void RememberCore(uint crc32, long timestamp)
        {
            _seen[crc32] = timestamp;
            TrimCache();
        }

        private void CleanupExpired(long timestamp)
        {
            List<uint>? expired = null;
            foreach (var item in _seen)
            {
                if (timestamp - item.Value <= _windowTimestampTicks)
                {
                    continue;
                }

                expired ??= [];
                expired.Add(item.Key);
            }

            if (expired == null)
            {
                return;
            }

            foreach (var key in expired)
            {
                _seen.Remove(key);
            }
        }

        private void TrimCache()
        {
            while (_seen.Count > _maxCacheSize)
            {
                var oldestKey = default(uint);
                var oldestTimestamp = long.MaxValue;
                foreach (var item in _seen)
                {
                    if (item.Value >= oldestTimestamp)
                    {
                        continue;
                    }

                    oldestKey = item.Key;
                    oldestTimestamp = item.Value;
                }

                _seen.Remove(oldestKey);
            }
        }
    }
}

public sealed class BroadcastingFeatureOptions
{
    public static BroadcastingFeatureOptions Default { get; } = new();
    public bool IsLoopProtectionEnabled { get; init; }
    public TimeSpan LoopProtectionWindow { get; init; } = TimeSpan.FromSeconds(10);
    public int MaxLoopProtectionCacheSize { get; init; } = 4096;
}
