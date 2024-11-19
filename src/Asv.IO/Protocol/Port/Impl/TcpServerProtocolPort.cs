using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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

    public static TcpServerProtocolPortConfig Parse(PortArgs args)
    {
        return new TcpServerProtocolPortConfig()
        {
            Host = args.Host ?? "127.0.0.1",
            Port = args.Port ?? 7341,
            
        };
    }
}

public class TcpServerProtocolPort:ProtocolPort
{
    public const string Scheme = "tcp_s";
    public static PortTypeInfo Info => new(Scheme, "Tcp server port");
    
    private readonly TcpServerProtocolPortConfig _config;
    private readonly IProtocolCore _core;
    private Socket? _socket;
    private Thread? _listenThread;
    private readonly ILogger<TcpServerProtocolPort> _logger;
    private readonly ImmutableArray<ParserFactoryDelegate> _parserFactory;
    private readonly ImmutableArray<IProtocolProcessingFeature> _features;

    public TcpServerProtocolPort(
        TcpServerProtocolPortConfig config, 
        ImmutableArray<IProtocolProcessingFeature> features, 
        ImmutableArray<ParserFactoryDelegate> parserFactory,
        IProtocolCore core) 
        : base($"{Scheme}_{config.Host}_{config.Port}", config, features, core)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(parserFactory);
        ArgumentNullException.ThrowIfNull(core);
        _config = config;
        _features = features;
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
                    InternalAddConnection(new SocketProtocolConnection( 
                        socket,
                        $"{Id}_{_socket.RemoteEndPoint}",
                        _config,[.._parserFactory.Select(x=>x(_core))],_features,_core));
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

public static class TcpServerProtocolPortHelper
{
    public static void RegisterTcpServerPort(this IProtocolBuilder builder)
    {
        builder.RegisterPort(TcpServerProtocolPort.Info, 
            (args, features, parserFactory,core) 
                => new TcpServerProtocolPort(TcpServerProtocolPortConfig.Parse(args), features, parserFactory,core));
    }
}