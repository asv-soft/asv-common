using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public class TcpClientProtocolPortConfig(Uri connectionString) : ProtocolPortConfig(connectionString)
{
    public static TcpClientProtocolPortConfig CreateDefault() => new(new Uri($"{TcpClientProtocolPort.Scheme}://127.0.0.1:7341"));
}

public class TcpClientProtocolPort:ProtocolPort<TcpClientProtocolPortConfig>
{
    public const string Scheme = "tcp";
    public static readonly PortTypeInfo Info  = new(Scheme, "Tcp client port");
    
    private readonly TcpClientProtocolPortConfig _config;
    private readonly IProtocolContext _context;
    private readonly IPEndPoint _remoteEndpoint;
    private SocketProtocolEndpoint? _endpoint;
    private Socket? _socket;

    public TcpClientProtocolPort(
        TcpClientProtocolPortConfig config, 
        IProtocolContext context,
        IStatisticHandler statistic) 
        : base(ProtocolHelper.NormalizeId($"{Scheme}_{config.Host}_{config.Port}"), config, context, true, statistic)
    {
        _config = config;
        _context = context;
        _remoteEndpoint = _config.CheckAndGetLocalHost();
        ArgumentNullException.ThrowIfNull(config);
    }

    

    public override PortTypeInfo TypeInfo => Info;
    protected override void InternalSafeDisable()
    {
        if (_socket != null)
        {
            _socket.Dispose();
            _socket = null;
        }
        if (_endpoint != null)
        {
            _endpoint.Dispose();
            _endpoint = null;
        }
    }

    protected override void InternalSafeEnable(CancellationToken token)
    {
        _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        _socket.Connect(_remoteEndpoint);
        _socket.SendBufferSize = _config.SendBufferSize;
        _socket.SendTimeout = _config.SendTimeout;
        _socket.ReceiveBufferSize = _config.ReadBufferSize;
        _socket.ReceiveTimeout = _config.ReadTimeout;
        
        _endpoint = new SocketProtocolEndpoint(
            _socket,
            ProtocolHelper.NormalizeId($"{Id}_{_socket.RemoteEndPoint}"),
            _config, InternalCreateParsers(), _context, StatisticHandler);
        InternalAddConnection(_endpoint);
    }

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        InternalSafeDisable();
        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        InternalSafeDisable();
        await base.DisposeAsyncCore();
    }

    #endregion
}

public static class TcpClientProtocolPortHelper
{
    public static IProtocolPort AddTcpClientPort(this IProtocolRouter src, Action<TcpClientProtocolPortConfig> edit)
    {
        var cfg = TcpClientProtocolPortConfig.CreateDefault();
        edit(cfg);
        return src.AddPort(cfg.AsUri());
    }
    public static void RegisterTcpClientPort(this IProtocolBuilder builder)
    {
        builder.RegisterPort(TcpClientProtocolPort.Info, 
            (cs,  context,stat) 
                => new TcpClientProtocolPort(new TcpClientProtocolPortConfig(cs), context,stat));
    }
}
