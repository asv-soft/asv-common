using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
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
    public const string Scheme = "tcps";
    public static PortTypeInfo Info => new(Scheme, "Tcp server port");
    
    private readonly TcpServerProtocolPortConfig _config;
    private readonly IProtocolCore _core;
    private Socket? _socket;
    private Thread? _listenThread;
    private readonly ILogger<TcpServerProtocolPort> _logger;
    private readonly ImmutableArray<IProtocolFeature> _features;
    private readonly ChannelWriter<IProtocolMessage> _rxChannel;
    private readonly ChannelWriter<ProtocolException> _errorChannel;

    public TcpServerProtocolPort(
        TcpServerProtocolPortConfig config, 
        ImmutableArray<IProtocolFeature> features, 
        ChannelWriter<IProtocolMessage> rxChannel, 
        ChannelWriter<ProtocolException> errorChannel,
        ImmutableDictionary<string, ParserFactoryDelegate> parsers,
        ImmutableArray<ProtocolInfo> protocols,
        IProtocolCore core,
        IStatisticHandler statistic) 
        : base(ProtocolHelper.NormalizeId($"{Scheme}_{config.Host}_{config.Port}"), config, features,rxChannel,errorChannel, parsers, protocols, core,statistic)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(core);
        _config = config;
        _features = features;
        _rxChannel = rxChannel;
        _errorChannel = errorChannel;
        _core = core;
        _logger = core.LoggerFactory.CreateLogger<TcpServerProtocolPort>();
    }

    public override PortTypeInfo TypeInfo => Info;


    protected override void InternalSafeEnable(CancellationToken token)
    {
        _socket?.Close();
        _socket?.Dispose();
        
        _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        _socket.Bind(new IPEndPoint(IPAddress.Parse(_config.Host), _config.Port));
        _socket.Listen(_config.MaxConnection);
        _listenThread = new Thread(AcceptNewEndpoint) { IsBackground = true, Name = $"WAIT_CLIENTS_{Id}" };
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
                    InternalAddConnection(new SocketProtocolEndpoint( 
                        socket,
                        ProtocolHelper.NormalizeId($"{Id}_{_socket.RemoteEndPoint}"),
                        _config,InternalCreateParsers(),_features,_rxChannel,_errorChannel,_core,StatisticHandler));
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
        builder.RegisterPortType(TcpServerProtocolPort.Info, 
            (args, features, rx, error, parsers,protocols,core,stat) 
                => new TcpServerProtocolPort(TcpServerProtocolPortConfig.Parse(args), features, rx, error, parsers, protocols, core,stat));
    }
}