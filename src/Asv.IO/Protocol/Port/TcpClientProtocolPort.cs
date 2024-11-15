using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public class TcpClientProtocolPortConfig:ProtocolPortConfig
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 7341;
}

public class TcpClientProtocolPort:ProtocolPort
{
    public const string Scheme = "tcp_c";
    
    private readonly TcpClientProtocolPortConfig _config;
    private readonly IPipeCore _core;
    private readonly IEnumerable<IProtocolRouteFilter> _filters;
    private readonly Func<IEnumerable<IProtocolParser>> _parserFactory;
    private Socket? _socket;
    
    public TcpClientProtocolPort(TcpClientProtocolPortConfig config, IPipeCore core, IEnumerable<IProtocolRouteFilter> filters, Func<IEnumerable<IProtocolParser>> parserFactory) 
        : base($"{Scheme}_{config.Host}_{config.Port}", config, core)
    {
        _config = config;
        _core = core;
        _filters = filters;
        _parserFactory = parserFactory;
        ArgumentNullException.ThrowIfNull(config);
    }

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
        InternalAddConnection(new SocketProtocolConnection(_socket,$"{Id}_{_socket.RemoteEndPoint}", _config,_parserFactory(),_filters, _core));
    }

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _socket?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_socket is IAsyncDisposable socketAsyncDisposable)
            await socketAsyncDisposable.DisposeAsync();
        else if (_socket != null)
            _socket.Dispose();

        await base.DisposeAsyncCore();
    }

    #endregion
}
