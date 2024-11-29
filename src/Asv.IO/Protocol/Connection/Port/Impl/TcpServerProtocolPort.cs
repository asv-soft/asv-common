using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Asv.IO;

public class TcpServerProtocolPortConfig(Uri connectionString) : ProtocolPortConfig(connectionString)
{
    public static TcpServerProtocolPortConfig CreateDefault() => new(new Uri($"{TcpServerProtocolPort.Scheme}://127.0.0.1:7341"));

    public const string MaxConnectionKey = "maxConnection";
    public int? MaxConnection
    {
        get
        {
            var maxConnection = Query.Get(MaxConnectionKey);
            if (string.IsNullOrWhiteSpace(maxConnection) || !int.TryParse(maxConnection, out var port))
            {
                return null;
            }
            return port;
        }
        set
        {
            if (value.HasValue)
            {
                Query.Set(MaxConnectionKey, value.Value.ToString());
            }
            else
            {
                Query.Remove(MaxConnectionKey);
            }
        }
    }
}

public class TcpServerProtocolPort:ProtocolPort<TcpServerProtocolPortConfig>
{
    public const string Scheme = "tcps";
    public static PortTypeInfo Info => new(Scheme, "Tcp server port");

    private readonly TcpServerProtocolPortConfig _config;
    private readonly IProtocolContext _context;
    private Socket? _socket;
    private readonly ILogger<TcpServerProtocolPort> _logger;
    private readonly IPEndPoint _bindEndpoint;

    public TcpServerProtocolPort(
        TcpServerProtocolPortConfig config, 
        IProtocolContext context,
        IStatisticHandler statistic) 
        : base(ProtocolHelper.NormalizeId($"{Scheme}_{config.Host}_{config.Port}"), config, context, false, statistic)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(context);
        _config = config;
        _context = context;
        _logger = context.LoggerFactory.CreateLogger<TcpServerProtocolPort>();
        _bindEndpoint = config.CheckAndGetLocalHost();
    }

    public override PortTypeInfo TypeInfo => Info;


    protected override void InternalSafeEnable(CancellationToken token)
    {
        _socket?.Close();
        _socket?.Dispose();
        
        _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        _socket.Bind(_bindEndpoint);
        if (_config.MaxConnection == null)
        {
            _socket.Listen();
        }
        else
        {
            _socket.Listen(_config.MaxConnection.Value);
        }
        Task.Factory.StartNew(AcceptNewEndpoint,token,token, TaskCreationOptions.LongRunning,TaskScheduler.Default);
    }
    private void AcceptNewEndpoint(object? state)
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var cancel = (CancellationToken) state!;
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
                        _config,InternalCreateParsers(),_context,StatisticHandler));
                }
                catch (Exception ex)
                {
                    _logger.ZLogError(ex, $"Unhandled exception:{ex.Message}");
                    Debug.Assert(false);
                    InternalRisePortErrorAndReconnect(ex);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogDebug(ex, $"Unhandled exception on {nameof(AcceptNewEndpoint)}:{ex.Message}");
            InternalRisePortErrorAndReconnect(ex);
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
    public static IProtocolPort AddTcpServerPort(this IProtocolRouter src, Action<TcpServerProtocolPortConfig> edit)
    {
        var cfg = TcpServerProtocolPortConfig.CreateDefault();
        edit(cfg);
        return src.AddPort(cfg.AsUri());
    }
    public static void RegisterTcpServerPort(this IProtocolBuilder builder)
    {
        builder.RegisterPort(TcpServerProtocolPort.Info, 
            (cs, context,stat) 
                => new TcpServerProtocolPort(new TcpServerProtocolPortConfig(cs), context,stat));
    }
}