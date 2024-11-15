using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Asv.IO;


public class TcpServerProtocolPortConfig:ProtocolPortConfig
{
    public const string Scheme = "tcp_s";
    
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 7341;
    public int MaxConnection { get; set; } = 100;
}

public class TcpServerProtocolPort:ProtocolPort
{
    public const string Scheme = "tcp_s";
    
    private readonly TcpServerProtocolPortConfig _config;
    private readonly IEnumerable<IProtocolRouteFilter> _filters;
    private readonly Func<IEnumerable<IProtocolParser>> _parserFactory;
    private readonly IPipeCore _core;
    private Socket? _socket;
    private Thread? _listenThread;
    private readonly ILogger<TcpServerProtocolPort> _logger;

    public TcpServerProtocolPort(TcpServerProtocolPortConfig config, IEnumerable<IProtocolRouteFilter> filters, Func<IEnumerable<IProtocolParser>> parserFactory, IPipeCore core) 
        : base($"{Scheme}_{config.Host}_{config.Port}", config, core)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(filters);
        ArgumentNullException.ThrowIfNull(parserFactory);
        ArgumentNullException.ThrowIfNull(core);
        _config = config;
        _filters = filters;
        _parserFactory = parserFactory;
        _core = core;
        _logger = core.LoggerFactory.CreateLogger<TcpServerProtocolPort>();
    }

    
    protected override void InternalSafeEnable(CancellationToken token)
    {
        _socket?.Close();
        _socket?.Dispose();
        
        _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        _socket.Bind(new IPEndPoint(IPAddress.Parse(_config.Host), _config.Port));
        _socket.Listen(_config.MaxConnection);
        _listenThread = new Thread(AcceptNewEndpoint) { IsBackground = true, Name = Id };
        _listenThread.Start(token);
        
    }
    private void AcceptNewEndpoint(object? state)
    {
        var cancel = (CancellationToken)(state ?? throw new ArgumentNullException(nameof(state)));
        try
        {
            while (_socket != null && cancel is { IsCancellationRequested: false })
            {
                try
                {
                    var socket = _socket.Accept();
                    InternalAddConnection(new SocketProtocolConnection( socket,$"{Id}_{_socket.RemoteEndPoint}",_config,_parserFactory(),_filters,_core));
                }
                catch (Exception ex)
                {
                    _logger.ZLogError(ex, $"Unhandled exception:{ex.Message}");
                    Debug.Assert(false);
                    InternalPublishError(ex);
                }
            }
        }
        catch (ThreadAbortException ex)
        {
            _logger.ZLogDebug(ex, $"Thread abort exception:{ex.Message}");
            InternalPublishError(ex);
        }
    }
    protected override void InternalSafeDisable()
    {
        if (_socket != null)
        {
            var socket = _socket;
            socket.Close();
            socket.Dispose();
            _socket = null;
        }
    }

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (_socket != null)
        {
            var socket = _socket;
            socket.Close();
            socket.Dispose();
            _socket = null;
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_socket != null)
        {
            var socket = _socket;
            socket.Close();
            socket.Dispose();
            _socket = null;
        }

        await base.DisposeAsyncCore();
    }

    #endregion
    
    
}