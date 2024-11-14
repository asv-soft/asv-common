using System;
using System.Net.Sockets;
using System.Threading;

namespace Asv.IO;



public class TcpClientPipePort:PipePort
{
    private readonly TcpPipePortConfig _config;
    private readonly IPipeCore _core;
    private Socket? _socket;
    private readonly string _id;

    public TcpClientPipePort(TcpPipePortConfig config, IPipeCore core) 
        : base(config, core)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.Validate();
        _config = config;
        _core = core;
        _id = $"tcp_c://{config.Host}:{config.Port}";
    }

    public override string Id => _id;

    protected override void InternalSafeDisable()
    {
        if (_socket != null)
        {
            _socket?.Close();
            _socket?.Dispose();
            _socket = null;
        }
    }

    protected override void InternalSafeEnable(CancellationToken token)
    {
        _socket?.Close();
        _socket?.Dispose();
        _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        _socket.Connect(_config.Host,_config.Port);
        InternalAddPipe(new TcpSocketEndpoint(_config,this, _socket,_core));
    }
}
