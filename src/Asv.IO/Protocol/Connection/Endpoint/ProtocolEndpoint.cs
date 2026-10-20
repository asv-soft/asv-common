using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;

namespace Asv.IO;

public abstract class ProtocolEndpoint : ProtocolConnection, IProtocolEndpoint
{
    private readonly TimeSpan _readEmptyLoopDelay;
    private readonly ImmutableArray<IProtocolParser> _parsers;
    private readonly Channel<IProtocolMessage> _txChannel;
    private readonly Channel<IProtocolMessage> _rxChannel;
    private readonly ILogger<ProtocolEndpoint> _logger;
    private readonly IDisposable _parserSub;
    private readonly ReactiveProperty<ProtocolConnectionException?> _lastError = new(null);
    private readonly ImmutableHashSet<string> _parserAvailable;
    private readonly ProtocolEncoderSet _encoders;
    private readonly Lock _lifecycleSync = new();
    private readonly Lock _pendingRxSync = new();
    private readonly TaskCompletionSource _stopped = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );
    private IProtocolMessage? _pendingRxMessage;
    private Task _loops = Task.CompletedTask;
    private bool _started;
    private bool _stopping;
    private int _failed;

    protected ProtocolEndpoint(
        string id,
        ProtocolPortConfig config,
        ImmutableArray<IProtocolParser> parsers,
        IProtocolContext context,
        IStatisticHandler? statistic = null
    )
        : this(id, config, parsers, context, statistic, true) { }

    /// <summary>Allows transports to finish initialization before starting their readers.</summary>
    protected ProtocolEndpoint(
        string id,
        ProtocolPortConfig config,
        ImmutableArray<IProtocolParser> parsers,
        IProtocolContext context,
        IStatisticHandler? statistic,
        bool startImmediately
    )
        : base(id, context, statistic)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(context);
        _readEmptyLoopDelay = TimeSpan.FromMilliseconds(config.ReadEmptyLoopDelayMs);
        _logger = context.LoggerFactory.CreateLogger<ProtocolEndpoint>();
        _parsers = parsers;
        _encoders = new ProtocolEncoderSet(context);
        _parserAvailable = _parsers.Select(x => x.Info.Id).ToImmutableHashSet();
        _txChannel =
            config.TxQueueSize <= 0
                ? Channel.CreateUnbounded<IProtocolMessage>(
                    new UnboundedChannelOptions { SingleReader = true }
                )
                : Channel.CreateBounded<IProtocolMessage>(
                    new BoundedChannelOptions(config.TxQueueSize)
                    {
                        SingleReader = true,
                        SingleWriter = false,
                        FullMode = config.DropMessageWhenFullTxQueue
                            ? BoundedChannelFullMode.DropOldest
                            : BoundedChannelFullMode.Wait,
                    },
                    TxMessageDropped
                );
        _rxChannel =
            config.RxQueueSize <= 0
                ? Channel.CreateUnbounded<IProtocolMessage>(
                    new UnboundedChannelOptions { SingleReader = true }
                )
                : Channel.CreateBounded<IProtocolMessage>(
                    new BoundedChannelOptions(config.RxQueueSize)
                    {
                        SingleReader = true,
                        SingleWriter = false,
                        FullMode = config.DropMessageWhenFullRxQueue
                            ? BoundedChannelFullMode.DropOldest
                            : BoundedChannelFullMode.Wait,
                    },
                    RxMessageDropped
                );
        var subscriptions = Disposable.CreateBuilder();
        foreach (var parser in _parsers)
        {
            parser.OnMessage.Subscribe(OnParsedMessage).AddTo(ref subscriptions);
        }
        _parserSub = subscriptions.Build();
        if (startImmediately)
        {
            Start();
        }
    }

    protected void Start()
    {
        lock (_lifecycleSync)
        {
            if (_started || IsDisposed || _stopping)
            {
                return;
            }
            _started = true;
            _loops = Task.WhenAll(
                Task.Run(ReadLoopAsync),
                Task.Run(WriteLoopAsync),
                Task.Run(PublishRxLoopAsync)
            );
        }
    }

    public IEnumerable<IProtocolParser> Parsers => _parsers;
    public ReadOnlyReactiveProperty<ProtocolConnectionException?> LastError => _lastError;
    internal Task Shutdown => _stopped.Task;

    private void OnParsedMessage(IProtocolMessage message)
    {
        if (
            IsDisposed
            || ConnectionCancel.IsCancellationRequested
            || _rxChannel.Writer.TryWrite(message)
        )
        {
            return;
        }
        lock (_pendingRxSync)
        {
            if (_pendingRxMessage == null)
            {
                // Push is synchronous. The read loop awaits this one pending message
                // before supplying another byte, so no reactive backlog is created.
                _pendingRxMessage = message;
                return;
            }
        }
        Fail(
            new InvalidOperationException(
                "The parser emitted messages faster than its owning read loop can accept them."
            ),
            "receive"
        );
    }

    private ValueTask FlushParsedMessageAsync()
    {
        IProtocolMessage? message;
        lock (_pendingRxSync)
        {
            message = _pendingRxMessage;
            _pendingRxMessage = null;
        }
        return message == null
            ? ValueTask.CompletedTask
            : _rxChannel.Writer.WriteAsync(message, ConnectionCancel);
    }

    private void RxMessageDropped(IProtocolMessage message)
    {
        StatisticHandler.IncrementDropRxMessage();
        _logger.ZLogWarning(
            $"Dropped message (rx queue is full) {message.Protocol.Id} {message.Name}"
        );
    }

    private void TxMessageDropped(IProtocolMessage message)
    {
        StatisticHandler.IncrementDropTxMessage();
        _logger.ZLogWarning(
            $"Dropped message (tx queue is full) {message.Protocol.Id} {message.Name}"
        );
    }

    private async Task PublishRxLoopAsync()
    {
        try
        {
            while (await _rxChannel.Reader.WaitToReadAsync(ConnectionCancel).ConfigureAwait(false))
            {
                while (_rxChannel.Reader.TryRead(out var message))
                {
                    ConnectionCancel.ThrowIfCancellationRequested();
                    StatisticHandler.IncrementRxMessage();
                    await InternalPublishRxMessageAsync(message, ConnectionCancel)
                        .ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (ConnectionCancel.IsCancellationRequested) { }
        catch (ChannelClosedException) when (ConnectionCancel.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Fail(ex, "receive publication");
        }
    }

    private async Task ReadLoopAsync()
    {
        try
        {
            while (!ConnectionCancel.IsCancellationRequested)
            {
                var bufferSize = GetAvailableBytesToRead();
                if (bufferSize == 0)
                {
                    await Task.Delay(_readEmptyLoopDelay, Context.TimeProvider, ConnectionCancel)
                        .ConfigureAwait(false);
                    continue;
                }
                var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                try
                {
                    var readBytes = await InternalRead(
                            buffer.AsMemory(0, bufferSize),
                            ConnectionCancel
                        )
                        .ConfigureAwait(false);
                    if ((uint)readBytes > (uint)bufferSize)
                    {
                        throw new InvalidOperationException(
                            "The transport returned an invalid read count."
                        );
                    }
                    StatisticHandler.AddRxBytes(readBytes);
                    for (var i = 0; i < readBytes; i++)
                    {
                        ConnectionCancel.ThrowIfCancellationRequested();
                        for (var j = 0; j < _parsers.Length; j++)
                        {
                            var parsed = _parsers[j].Push(buffer[i]);
                            if (Volatile.Read(ref _pendingRxMessage) != null)
                            {
                                await FlushParsedMessageAsync().ConfigureAwait(false);
                            }
                            if (parsed)
                            {
                                foreach (var parser in _parsers)
                                {
                                    parser.Reset();
                                }
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
        catch (OperationCanceledException) when (ConnectionCancel.IsCancellationRequested) { }
        catch (ChannelClosedException) when (ConnectionCancel.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Fail(ex, "read");
        }
    }

    protected abstract int GetAvailableBytesToRead();
    protected abstract ValueTask<int> InternalRead(Memory<byte> memory, CancellationToken cancel);
    protected abstract ValueTask<int> InternalWrite(
        ReadOnlyMemory<byte> memory,
        CancellationToken cancel
    );

    private async Task WriteLoopAsync()
    {
        try
        {
            while (await _txChannel.Reader.WaitToReadAsync(ConnectionCancel).ConfigureAwait(false))
            {
                while (_txChannel.Reader.TryRead(out var message))
                {
                    ConnectionCancel.ThrowIfCancellationRequested();
                    if (!_parserAvailable.Contains(message.Protocol.Id))
                    {
                        continue;
                    }
                    var filtered = await InternalFilterTxMessage(message).ConfigureAwait(false);
                    if (filtered == null)
                    {
                        continue;
                    }
                    ConnectionCancel.ThrowIfCancellationRequested();
                    var encoder = _encoders.Get(filtered.Protocol.Id);
                    var size = ProtocolEncodingMixin.GetMaxByteSize(encoder, filtered);
                    var buffer = ArrayPool<byte>.Shared.Rent(size);
                    try
                    {
                        var written = ProtocolEncodingMixin.EncodeChecked(
                            encoder,
                            filtered,
                            buffer.AsSpan(0, size)
                        );
                        var sent = await InternalWrite(
                                buffer.AsMemory(0, written),
                                ConnectionCancel
                            )
                            .ConfigureAwait(false);
                        if (sent != written)
                        {
                            throw new InvalidOperationException(
                                $"Incomplete frame write: expected {written} bytes, sent {sent}."
                            );
                        }
                        StatisticHandler.AddTxBytes(sent);
                        StatisticHandler.IncrementTxMessage();
                        InternalPublishTxMessage(filtered);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (ConnectionCancel.IsCancellationRequested) { }
        catch (ChannelClosedException) when (ConnectionCancel.IsCancellationRequested) { }
        catch (Exception) when (ConnectionCancel.IsCancellationRequested) { }
        catch (Exception ex)
        {
            StatisticHandler.IncrementTxError();
            Fail(ex, "write");
        }
    }

    private void Fail(Exception exception, string operation)
    {
        if (IsDisposed || Interlocked.Exchange(ref _failed, 1) != 0)
        {
            return;
        }
        var error = new ProtocolConnectionException(
            this,
            $"Error during {operation}: {exception.Message}",
            exception
        );
        _logger.ZLogError(exception, $"Error during {operation} on {Id}");
        _txChannel.Writer.TryComplete(error);
        _rxChannel.Writer.TryComplete(error);
        if (operation == "write")
        {
            InternalPublishTxError(error);
        }
        else
        {
            StatisticHandler.IncrementRxError();
            InternalPublishRxError(error);
        }
        if (!IsDisposed)
        {
            _lastError.OnNext(error);
        }
        if (!IsDisposed)
        {
            CancelPendingOperations();
        }
    }

    public override ValueTask Send(IProtocolMessage message, CancellationToken cancel = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancel.ThrowIfCancellationRequested();
        if (IsDisposed || Volatile.Read(ref _failed) != 0)
        {
            return ValueTask.CompletedTask;
        }
        return _txChannel.Writer.WriteAsync(message, cancel);
    }

    private Task StopAsync()
    {
        Task loops;
        lock (_lifecycleSync)
        {
            if (_stopping)
            {
                return _stopped.Task;
            }
            _stopping = true;
            loops = _loops;
        }
        _txChannel.Writer.TryComplete();
        _rxChannel.Writer.TryComplete();
        CancelPendingOperations();
        _ = FinishStopAsync(loops);
        return _stopped.Task;
    }

    private async Task FinishStopAsync(Task loops)
    {
        try
        {
            await loops.ConfigureAwait(false);
            List<Exception>? errors = null;
            try
            {
                _parserSub.Dispose();
            }
            catch (Exception ex)
            {
                (errors ??= []).Add(ex);
            }
            foreach (var parser in _parsers)
            {
                try
                {
                    await parser.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    (errors ??= []).Add(ex);
                }
            }
            while (_rxChannel.Reader.TryRead(out _)) { }
            while (_txChannel.Reader.TryRead(out _)) { }
            lock (_pendingRxSync)
            {
                _pendingRxMessage = null;
            }
            _encoders.Clear();
            _lastError.Dispose();
            if (errors != null)
            {
                throw new AggregateException(errors);
            }
            _stopped.TrySetResult();
        }
        catch (Exception ex)
        {
            _stopped.TrySetException(ex);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            var stopped = StopAsync();
            if (!stopped.IsCompletedSuccessfully)
            {
                _ = ObserveStopAsync(stopped);
            }
        }
        base.Dispose(disposing);
    }

    private async Task ObserveStopAsync(Task stopped)
    {
        try
        {
            await stopped.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Error stopping endpoint {Id}");
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        try
        {
            await StopAsync().ConfigureAwait(false);
        }
        finally
        {
            await base.DisposeAsyncCore().ConfigureAwait(false);
        }
    }

    public override string ToString() => $"[ENDPOINT]({Id})";
}
