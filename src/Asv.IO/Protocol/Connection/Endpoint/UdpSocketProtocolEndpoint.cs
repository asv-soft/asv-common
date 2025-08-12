using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public class UdpSocketProtocolEndpoint(
    Socket socket,
    IPEndPoint recvAddr,
    string id,
    ProtocolPortConfig config,
    ImmutableArray<IProtocolParser> parsers,
    IProtocolContext context,
    IStatisticHandler statisticHandler)
    : ProtocolEndpoint(id, config, parsers, context, statisticHandler)
{
    private readonly IProtocolContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private byte[] _buffer = [];
    private int _readSize;
    private readonly AutoResetEvent _waitDataRead = new(false);
    private long _lastDataReceivedOrSentSuccess = context.TimeProvider.GetTimestamp();
    private readonly TimeSpan _reconnectTimeout = config.ReconnectTimeoutMs <= 0 ?
        Timeout.InfiniteTimeSpan :
        TimeSpan.FromMilliseconds(config.ReconnectTimeoutMs);

    public IPEndPoint RemoteEndPoint { get; } = recvAddr;

    protected override int GetAvailableBytesToRead()
    {
        var available = _readSize;
        if (available > 0)
        {
            _lastDataReceivedOrSentSuccess = _context.TimeProvider.GetTimestamp();
            return available;
        }

        // If no data is available, check if the socket is still connected
        if (_reconnectTimeout != Timeout.InfiniteTimeSpan 
            && _context.TimeProvider.GetElapsedTime(_lastDataReceivedOrSentSuccess) > _reconnectTimeout)
        {
            throw new TimeoutException($"UDP socket didn't send or receive any data with {_reconnectTimeout}");
        }
        return 0;
    }

    protected override ValueTask<int> InternalRead(Memory<byte> memory, CancellationToken cancel)
    {
        var span = new Span<byte>(_buffer, 0, _readSize);
        Debug.Assert(memory.Length >= _readSize);
        span.CopyTo(memory.Span);
        _readSize = 0; // Reset read size to indicate that data has been consumed
        _waitDataRead.Set();
        return ValueTask.FromResult(span.Length);
    }

    protected override ValueTask<int> InternalWrite(ReadOnlyMemory<byte> memory, CancellationToken cancel)
    {
        var count = socket.SendToAsync(memory, SocketFlags.None, RemoteEndPoint,cancel);
        _lastDataReceivedOrSentSuccess = _context.TimeProvider.GetTimestamp();
        return count;
    }

    internal void ApplyData(byte[] buffer, int readSize)
    {
        _buffer = buffer;
        _readSize = readSize;
        _waitDataRead.WaitOne();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _waitDataRead.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        _waitDataRead.Dispose();
        await base.DisposeAsyncCore();
    }
}