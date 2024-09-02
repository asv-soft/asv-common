using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;

namespace Asv.IO.Protocol;

public class TcpServerConnection:CombinedProtocolConnection
{
    private TcpListener _tcp;
    private CancellationTokenSource? _disableCancel;

    public TcpServerConnection()
    {
        _tcp = new TcpListener(IPAddress.Parse(_cfg.Host), _cfg.Port);
    }

    public void Enable()
    {
        Observable.
    }

    public void Disable()
    {
        try
        {
            _disableCancel?.Cancel(false);
            _disableCancel?.Dispose();
        }
        catch (Exception e)
        {
            
        }
    }
    
    public void TryReconnect()
    {
        
        _tcp.Start();
        _disableCancel = new CancellationTokenSource();
        Task.Factory.StartNew(AddNewClient, _disableCancel.Token, TaskCreationOptions.LongRunning);
    }

    private async void AddNewClient(object? obj)
    {
        while (_disableCancel?.IsCancellationRequested == false)
        {
            var client = await _tcp.AcceptTcpClientAsync(_disableCancel.Token);
            AddConnection(new TcpServerAcceptedClientConnection(client));
        }
    }

    protected override void AddConnection(IProtocolConnection connection)
    {
        throw new NotImplementedException();
    }

    protected override void RemoveConnection(IProtocolConnection connection)
    {
        throw new NotImplementedException();
    }
}

public class TcpServerAcceptedClientConnection:SimpleProtocolConnection
{
    private readonly TcpClient _client;

    public TcpServerAcceptedClientConnection(TcpClient client)
    {
        _client = client.DisposeItWith(Disposable);
    }

    protected override int BytesAvailable => _client.Available;

    protected override ValueTask<int> Read(Memory<byte> buffer, CancellationToken cancel = default)
    {
        return _client.GetStream().ReadAsync(buffer, cancel);
    }

    protected override ValueTask Write(ReadOnlyMemory<byte> buffer, CancellationToken cancel = default)
    {
        return _client.GetStream().WriteAsync(buffer, cancel);
    }
}