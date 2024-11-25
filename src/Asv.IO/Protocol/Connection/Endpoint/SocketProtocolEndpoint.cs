using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
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
    
    protected override int GetAvailableBytesToRead() => socket.Available;

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
            socket.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (socket is IAsyncDisposable socketAsyncDisposable)
            await socketAsyncDisposable.DisposeAsync();
        else
            socket.Dispose();

        await base.DisposeAsyncCore();
    }
    

    #endregion
}