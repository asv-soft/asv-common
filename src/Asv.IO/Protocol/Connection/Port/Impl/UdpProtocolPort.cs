using System;
using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Asv.IO;

public class UdpProtocolPortConfig(Uri connectionString) : ProtocolPortConfig(connectionString)
{
    public static UdpProtocolPortConfig CreateDefault() => new(new Uri($"{UdpProtocolPort.Scheme}://127.0.0.1:8080"));

    public const string RemoteHostKey = "rhost";
    public const string RemotePortKey = "rport";

    
    public IPEndPoint? GetRemoteEndpoint()
    {
        var remoteHost = Query.Get(RemoteHostKey);
        var remotePort = Query.Get(RemotePortKey);
        if (string.IsNullOrWhiteSpace(remoteHost) || !int.TryParse(remotePort, out var port))
        {
            return null;
        }
        return CheckIpEndpoint(remoteHost, port);
    }


   
}

public class UdpProtocolPort:ProtocolPort<UdpProtocolPortConfig>
{
    public const string Scheme = "udp";
    public static PortTypeInfo Info => new(Scheme, "Udp protocol port");
    private readonly UdpProtocolPortConfig _config;
    private readonly IProtocolContext _context;
    private readonly IPEndPoint _receiveEndPoint;
    private readonly IPEndPoint? _sendEndPoint;
    private Socket? _socket;
    private readonly ILogger<UdpProtocolPort> _logger;

    public UdpProtocolPort(
        UdpProtocolPortConfig config,
        IProtocolContext context,
        IStatisticHandler statistic) 
        : base(ProtocolHelper.NormalizeId($"{Scheme}_{config.Host}_{config.Port}"), config, context, true,statistic)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(context);
        _config = config;
        _context = context;
        _logger = context.LoggerFactory.CreateLogger<UdpProtocolPort>();

        _receiveEndPoint = config.CheckAndGetLocalHost();
        _sendEndPoint = config.GetRemoteEndpoint();
    }

    public override PortTypeInfo TypeInfo => Info;
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

    protected override void InternalSafeEnable(CancellationToken token)
    {
        _socket = new Socket(_receiveEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(_receiveEndPoint);
        if (_sendEndPoint == null)
        {
            Task.Factory.StartNew(AcceptNewEndpoint, token, TaskCreationOptions.LongRunning);
        }
        else
        {
            _socket.Connect(_sendEndPoint);
            InternalAddConnection(new SocketProtocolEndpoint(
                _socket,ProtocolHelper.NormalizeId($"{Id}_{_sendEndPoint}"), _config, InternalCreateParsers(), _context,StatisticHandler));
        }
    }
    
    private void AcceptNewEndpoint(object? state)
    {
        var cancel = (CancellationToken)(state ?? throw new ArgumentNullException(nameof(state)));
        try
        {
            if (_socket == null) return;
            if (cancel.IsCancellationRequested) return;
            var data = new byte[1];
            var span = new Span<byte>(data);
            EndPoint val = new IPEndPoint(IPAddress.Any, 0);
            _socket.ReceiveFrom(span, ref val);
            _socket.Connect(val);
            InternalAddConnection(new SocketProtocolEndpoint(
                _socket, ProtocolHelper.NormalizeId($"{Id}_{val}"), _config, InternalCreateParsers(), _context,StatisticHandler));
        }
        catch (ThreadAbortException ex)
        {
            _logger.ZLogDebug(ex, $"Thread abort exception:{ex.Message}");
            InternalRisePortErrorAndReconnect(ex);
        }
    }
}

public static class UdpProtocolPortHelper
{
    public static IProtocolPort AddUdpPort(this IProtocolRouter src, Action<UdpProtocolPortConfig> edit)
    {
        var cfg = UdpProtocolPortConfig.CreateDefault();
        edit(cfg);
        return src.AddPort(cfg.AsUri());
    }
    public static void RegisterUdpPort(this IProtocolBuilder builder)
    {
        builder.RegisterPortType(UdpProtocolPort.Info, 
            (cs, context,stat) 
                => new UdpProtocolPort(new UdpProtocolPortConfig(cs), context,stat));
    }
}

