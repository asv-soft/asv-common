using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public class TcpSocketEndpoint : PipeEndpoint
{
    private uint _txBytes;
    private uint _readBytes;
    private readonly Socket _socket;
    private readonly string _id;

    public TcpSocketEndpoint(PipeEndpointConfig config, IPipePort parent, Socket socket, IPipeCore core) 
        :base(parent,config,core)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(socket);
        config.Validate();
        _socket = socket;
        _id = $"{parent.Id}<={socket.RemoteEndPoint}";
    }
    public override string Id => _id;
    protected override async Task InternalWrite(PipeReader rdr, CancellationToken cancel)
    {
        if (IsDisposed) return;
        var result = await rdr.ReadAsync(cancel);
        var buffer = result.Buffer;
        if (buffer.IsEmpty) return;
        Interlocked.Add(ref _txBytes, (uint)buffer.Length);
        if (buffer.IsSingleSegment)
        {
            var sent = await _socket.SendAsync(buffer.First, cancel);
            Debug.Assert(sent == buffer.Length);
        }
        else
        {
            foreach (var memory in buffer)
            {
                var sent = await _socket.SendAsync(memory, cancel);
                Debug.Assert(sent == memory.Length);
            }
        }
        rdr.AdvanceTo(buffer.Start, buffer.End);
    }
    protected override async Task InternalRead(PipeWriter wrt, CancellationToken cancel)
    {
        if (IsDisposed) return;
        while (cancel.IsCancellationRequested && _socket.Available > 0)
        {
            var mem = wrt.GetMemory(_socket.Available);
            var readBytes = await _socket.ReceiveAsync(mem, cancel);
            Interlocked.Add(ref _readBytes, (uint)readBytes);
            wrt.Advance(readBytes);
        }
        await wrt.FlushAsync(cancel);
    }

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _socket.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_socket is IAsyncDisposable socketAsyncDisposable)
            await socketAsyncDisposable.DisposeAsync();
        else
            _socket.Dispose();

        await base.DisposeAsyncCore();
    }

    #endregion
    
}