using System;
using System.Collections.Immutable;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public abstract class TcpSocketProtocolEndpoint : ProtocolEndpoint
{
    private readonly Socket _socket;
    private long _lastDataReceivedOrSentSuccess;
    private readonly TimeSpan _reconnectTimeout;
    private readonly IProtocolContext _context;

    protected TcpSocketProtocolEndpoint(
        Socket socket,
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
        _lastDataReceivedOrSentSuccess = context.TimeProvider.GetTimestamp();
        _reconnectTimeout =
            config.ReconnectTimeoutMs <= 0
                ? Timeout.InfiniteTimeSpan
                : TimeSpan.FromMilliseconds(config.ReconnectTimeoutMs);
        Start();
    }

    protected override int GetAvailableBytesToRead()
    {
        if (_socket.Connected == false)
        {
            throw new InvalidOperationException("Socket is disconnected");
        }

        var available = _socket.Available;
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
                $"TCP socket didn't send or receive any data with {_reconnectTimeout}"
            );
        }

        return 0;
    }

    public Socket Socket => _socket;

    protected override async ValueTask<int> InternalRead(
        Memory<byte> memory,
        CancellationToken cancel
    )
    {
        return await _socket.ReceiveAsync(memory, cancel);
    }

    protected override async ValueTask<int> InternalWrite(
        ReadOnlyMemory<byte> memory,
        CancellationToken cancel
    )
    {
        var count = await _socket.SendAsync(memory, cancel);
        _lastDataReceivedOrSentSuccess = _context.TimeProvider.GetTimestamp();
        return count;
    }

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CancelPendingOperations();
            if (_socket.Connected)
            {
                _socket.Close();
            }

            _socket.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        CancelPendingOperations();
        if (_socket.Connected)
        {
            _socket.Close();
        }

        _socket.Dispose();
        await base.DisposeAsyncCore();
    }

    #endregion
}
