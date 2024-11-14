using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public class SocketProtocolConnection(
    Socket socket,
    string id,
    ProtocolConnectionConfig config,
    IEnumerable<IProtocolParser> parsers,
    IEnumerable<IProtocolRouteFilter> filters,
    IPipeCore core)
    : ProtocolConnection(id, config, parsers, filters, core)
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