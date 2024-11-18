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

public class UdpProtocolPortConfig:ProtocolPortConfig
{
    public string LocalHost { get; set; } = "127.0.0.1";
    public int LocalPort { get; set; } = 8080;
    public string? RemoteHost { get; set; }
    public int? RemotePort { get; set; }
}

public class UdpProtocolPort:ProtocolPort
{
    public const string Scheme = "udp";
    
    
    private readonly UdpProtocolPortConfig _config;
    private readonly IEnumerable<IProtocolProcessingFeature> _filters;
    private readonly Func<IEnumerable<IProtocolParser>> _parserFactory;
    private readonly IProtocolCore _core;
    private readonly IPEndPoint _receiveEndPoint;
    private readonly IPEndPoint? _sendEndPoint;
    private Socket? _socket;
    private readonly ILogger<UdpProtocolPort> _logger;

    public UdpProtocolPort(UdpProtocolPortConfig config, IEnumerable<IProtocolProcessingFeature> filters, Func<IEnumerable<IProtocolParser>> parserFactory, IProtocolCore core) 
        : base($"{Scheme}_{config.LocalHost}_{config.LocalPort}", config, core)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(filters);
        ArgumentNullException.ThrowIfNull(parserFactory);
        ArgumentNullException.ThrowIfNull(core);
        _config = config;
        _filters = filters;
        _parserFactory = parserFactory;
        _core = core;
        _logger = core.LoggerFactory.CreateLogger<UdpProtocolPort>();
        _receiveEndPoint = new IPEndPoint(IPAddress.Parse(config.LocalHost), config.LocalPort);
        if (!string.IsNullOrWhiteSpace(config.RemoteHost) && config.RemotePort.HasValue)
        {
            _sendEndPoint = new IPEndPoint(IPAddress.Parse(config.RemoteHost), config.RemotePort.Value);
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
            InternalAddConnection(new SocketProtocolConnection(_socket,$"{Id}_{_sendEndPoint}", _config,_parserFactory(),_filters, _core));
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
            InternalAddConnection(new SocketProtocolConnection(_socket,$"{Id}_{val}", _config,_parserFactory(),_filters, _core));
        }
        catch (ThreadAbortException ex)
        {
            _logger.ZLogDebug(ex, $"Thread abort exception:{ex.Message}");
            InternalPublishError(ex);
        }
    }
}