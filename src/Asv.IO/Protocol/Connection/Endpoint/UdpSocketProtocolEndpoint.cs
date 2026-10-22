using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public class UdpSocketProtocolEndpoint : ProtocolEndpoint
{
    private readonly IProtocolContext _context;
    private readonly Socket _socket;
    private readonly string _description;
    private readonly object _receiveSync = new();
    private byte[] _buffer = [];
    private int _readSize;
    private bool _stopping;
    private long _lastDataReceivedOrSentSuccess;
    private readonly TimeSpan _reconnectTimeout;
    private readonly CancellationTokenRegistration _receiveCancellation;

    public UdpSocketProtocolEndpoint(
        Socket socket,
        IPEndPoint recvAddr,
        string id,
        ProtocolPortConfig config,
        ImmutableArray<IProtocolParser> parsers,
        IProtocolContext context,
        IStatisticHandler statisticHandler
    )
        : base(id, config, parsers, context, statisticHandler, startImmediately: false)
    {
        _socket = socket;
        _context = context;
        RemoteEndPoint = recvAddr;
        try
        {
            _description = ProtocolConnectionDisplay.NetworkEndpointDescription(
                "UDP",
                id,
                config,
                parsers.Select(x => x.Info),
                socket.LocalEndPoint,
                recvAddr
            );
            _lastDataReceivedOrSentSuccess = context.TimeProvider.GetTimestamp();
            _reconnectTimeout =
                config.ReconnectTimeoutMs <= 0
                    ? Timeout.InfiniteTimeSpan
                    : TimeSpan.FromMilliseconds(config.ReconnectTimeoutMs);
            _receiveCancellation = ConnectionCancel.Register(StopReceiving);
            Start();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public IPEndPoint RemoteEndPoint { get; }
    public override string DisplayName => RemoteEndPoint?.ToString() ?? Id;
    public override string Description => _description ?? Id;

    protected override int GetAvailableBytesToRead()
    {
        var available = Volatile.Read(ref _readSize);
        if (available > 0)
        {
            _lastDataReceivedOrSentSuccess = _context.TimeProvider.GetTimestamp();
            return available;
        }

        // If no data is available, check if the socket is still connected
        if (
            _reconnectTimeout != Timeout.InfiniteTimeSpan
            && _context.TimeProvider.GetElapsedTime(_lastDataReceivedOrSentSuccess)
                > _reconnectTimeout
        )
        {
            throw new TimeoutException(
                $"UDP socket didn't send or receive any data with {_reconnectTimeout}"
            );
        }
        return 0;
    }

    protected override ValueTask<int> InternalRead(Memory<byte> memory, CancellationToken cancel)
    {
        lock (_receiveSync)
        {
            cancel.ThrowIfCancellationRequested();
            var span = new Span<byte>(_buffer, 0, _readSize);
            Debug.Assert(memory.Length >= _readSize, "Expected: memory.Length >= _readSize");
            span.CopyTo(memory.Span);
            var count = span.Length;
            _buffer = [];
            Volatile.Write(ref _readSize, 0);
            Monitor.PulseAll(_receiveSync);
            return ValueTask.FromResult(count);
        }
    }

    protected override ValueTask<int> InternalWrite(
        ReadOnlyMemory<byte> memory,
        CancellationToken cancel
    )
    {
        var count = _socket.SendToAsync(memory, SocketFlags.None, RemoteEndPoint, cancel);
        _lastDataReceivedOrSentSuccess = _context.TimeProvider.GetTimestamp();
        return count;
    }

    internal void ApplyData(byte[] buffer, int readSize)
    {
        if (readSize == 0)
        {
            return;
        }
        lock (_receiveSync)
        {
            while (_readSize != 0 && !_stopping)
            {
                Monitor.Wait(_receiveSync);
            }
            if (_stopping)
            {
                return;
            }
            _buffer = buffer;
            Volatile.Write(ref _readSize, readSize);
            while (_readSize != 0 && !_stopping)
            {
                Monitor.Wait(_receiveSync);
            }
        }
    }

    private void StopReceiving()
    {
        lock (_receiveSync)
        {
            _stopping = true;
            _buffer = [];
            Volatile.Write(ref _readSize, 0);
            Monitor.PulseAll(_receiveSync);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CancelPendingOperations();
            StopReceiving();
            _receiveCancellation.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        CancelPendingOperations();
        StopReceiving();
        _receiveCancellation.Dispose();
        await base.DisposeAsyncCore().ConfigureAwait(false);
    }
}
