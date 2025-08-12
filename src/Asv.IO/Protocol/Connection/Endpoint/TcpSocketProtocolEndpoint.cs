using System;
using System.Collections.Immutable;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public class TcpSocketProtocolEndpoint(
    Socket socket,
    string id,
    ProtocolPortConfig config,
    ImmutableArray<IProtocolParser> parsers,
    IProtocolContext context,
    IStatisticHandler statisticHandler)
    : ProtocolEndpoint(id, config, parsers, context,statisticHandler)
{
    private long _lastDataReceivedOrSentSuccess = context.TimeProvider.GetTimestamp();
    private readonly TimeSpan _reconnectTimeout = config.ReconnectTimeoutMs <= 0 ?
        Timeout.InfiniteTimeSpan :
        TimeSpan.FromMilliseconds(config.ReconnectTimeoutMs);
    
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
            _lastDataReceivedOrSentSuccess = _context.TimeProvider.GetTimestamp();
            return available;
        }

        // If no data is available, check if the socket is still connected
        if (_reconnectTimeout != Timeout.InfiniteTimeSpan 
            && _context.TimeProvider.GetElapsedTime(_lastDataReceivedOrSentSuccess) > _reconnectTimeout)
        {
            throw new TimeoutException($"TCP socket didn't send or receive any data with {_reconnectTimeout}");
        }
        
        return 0;
    }

    public Socket Socket => socket;
    
    protected override async ValueTask<int> InternalRead(Memory<byte> memory, CancellationToken cancel)
    {
        return await socket.ReceiveAsync(memory, cancel);
        //_logger.ZLogTrace($"RX:{BitConverter.ToString(memory.ToArray())}");
        //return result;
    }

    protected override async ValueTask<int> InternalWrite(ReadOnlyMemory<byte> memory, CancellationToken cancel)
    {
        var count = await socket.SendAsync(memory, cancel);
        _lastDataReceivedOrSentSuccess = _context.TimeProvider.GetTimestamp();
        return count;
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