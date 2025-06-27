using System;
using System.Collections.Immutable;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public class SocketProtocolEndpoint(
    Socket socket,
    string id,
    ProtocolPortConfig config,
    ImmutableArray<IProtocolParser> parsers,
    IProtocolContext context,
    IStatisticHandler statisticHandler)
    : ProtocolEndpoint(id, config, parsers, context,statisticHandler)
{
    private long _lastDataReceived = context.TimeProvider.GetTimestamp();
    private readonly TimeSpan _reconnectTimeout = TimeSpan.FromMilliseconds(config.ReconnectTimeoutMs);
    private readonly IProtocolContext _context = context;

    protected override int GetAvailableBytesToRead()
    {
        if (socket.Connected == false)
        {
            throw new InvalidOperationException("Socket is disconnected");
        }

        var available = socket.Available;
        if (available > 0)
        {
            _lastDataReceived = _context.TimeProvider.GetTimestamp();
            return available;
        }
        
        // this is for ping disconnected socket
        if (_context.TimeProvider.GetElapsedTime(_lastDataReceived) > _reconnectTimeout)
        {
            socket.Send(ReadOnlySpan<byte>.Empty);
        }
        if (socket.Connected == false)
        {
            throw new InvalidOperationException("Socket is not connected");
        }
        return 0;
    }

    protected override ValueTask<int> InternalRead(Memory<byte> memory, CancellationToken cancel)
    {
        return socket.ReceiveAsync(memory, cancel);
    }

    protected override ValueTask<int> InternalWrite(ReadOnlyMemory<byte> memory, CancellationToken cancel)
    {
        return socket.SendAsync(memory, cancel);
    }
    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (socket.Connected) socket.Close();
            socket.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (socket.Connected) socket.Close();
        socket.Dispose();
        await base.DisposeAsyncCore();
    }

    #endregion
}