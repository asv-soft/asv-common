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

public class UdpProtocolPortConfig:ProtocolPortConfig
{
    public string LocalHost { get; set; } = "127.0.0.1";
    public int LocalPort { get; set; } = 8080;
    public string? RemoteHost { get; set; }
    public int? RemotePort { get; set; }

    public static UdpProtocolPortConfig Parse(PortArgs args)
    {
        var rport = args.Query["rport"];
        if (int.TryParse(rport, out var port))
        {
            return new UdpProtocolPortConfig
            {
                LocalHost = args.Host ?? "127.0.0.1",
                LocalPort = args.Port ?? 7342,
                RemoteHost = args.Query["rhost"],
                RemotePort = port 
            };
        }

        return new UdpProtocolPortConfig
        {
            LocalHost = args.Host ?? "127.0.0.1",
            LocalPort = args.Port ?? 7342,
            RemoteHost = args.Query["rhost"],
        };

    }
}

public class UdpProtocolPort:ProtocolPort
{
    public const string Scheme = "udp";
    public static PortTypeInfo Info => new(Scheme, "Udp protocol port");
    
    private readonly UdpProtocolPortConfig _config;
    
    private readonly IProtocolCore _core;
    private readonly IPEndPoint _receiveEndPoint;
    private readonly IPEndPoint? _sendEndPoint;
    private Socket? _socket;
    private readonly ILogger<UdpProtocolPort> _logger;
    private readonly ImmutableArray<IProtocolFeature> _features;
    private readonly ChannelWriter<IProtocolMessage> _rxChannel;
    private readonly ChannelWriter<ProtocolException> _errorChannel;

    public UdpProtocolPort(
        UdpProtocolPortConfig config,
        ImmutableArray<IProtocolFeature> features, 
        ChannelWriter<IProtocolMessage> rxChannel, 
        ChannelWriter<ProtocolException> errorChannel,
        ImmutableDictionary<string, ParserFactoryDelegate> parsers,
        ImmutableArray<ProtocolInfo> protocols,
        IProtocolCore core,
        IStatisticHandler statistic) 
        : base(ProtocolHelper.NormalizeId($"{Scheme}_{config.LocalHost}_{config.LocalPort}"), config, features,rxChannel,errorChannel, parsers, protocols, core,statistic)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(core);
        _config = config;
        _features = features;
        _rxChannel = rxChannel;
        _errorChannel = errorChannel;
        _core = core;
        _logger = core.LoggerFactory.CreateLogger<UdpProtocolPort>();
        _receiveEndPoint = new IPEndPoint(IPAddress.Parse(config.LocalHost), config.LocalPort);
        if (!string.IsNullOrWhiteSpace(config.RemoteHost) && config.RemotePort.HasValue)
        {
            _sendEndPoint = new IPEndPoint(IPAddress.Parse(config.RemoteHost), config.RemotePort.Value);
        }
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
                _socket,ProtocolHelper.NormalizeId($"{Id}_{_sendEndPoint}"), _config, InternalCreateParsers(),_features, _rxChannel,_errorChannel, _core,StatisticHandler));
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
                _socket, ProtocolHelper.NormalizeId($"{Id}_{val}"), _config, InternalCreateParsers(),_features,_rxChannel,_errorChannel, _core,StatisticHandler));
        }
        catch (ThreadAbortException ex)
        {
            _logger.ZLogDebug(ex, $"Thread abort exception:{ex.Message}");
            InternalPublishError(ex);
        }
    }
}

public static class UdpProtocolPortHelper
{
    public static void RegisterUdpPort(this IProtocolBuilder builder)
    {
        builder.RegisterPortType(UdpProtocolPort.Info, 
            (args, features, rx, error, parsers,protocols,core,stat) 
                => new UdpProtocolPort(UdpProtocolPortConfig.Parse(args), features,rx,error, parsers, protocols, core,stat));
    }
}