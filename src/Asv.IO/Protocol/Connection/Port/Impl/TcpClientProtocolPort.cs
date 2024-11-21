using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public class TcpClientProtocolPortConfig:ProtocolPortConfig
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 7341;

    public static TcpClientProtocolPortConfig Parse(PortArgs args)
    {
        var config = new TcpClientProtocolPortConfig
        {
            Host = args.Host ?? "127.0.0.1",
            Port = args.Port ?? 7341
        };
        return config;
    }
}

public class TcpClientProtocolPort:ProtocolPort
{
    public const string Scheme = "tcp";
    public static readonly PortTypeInfo Info  = new(Scheme, "Tcp client port");
    
    private readonly TcpClientProtocolPortConfig _config;
    private readonly IProtocolCore _core;
    private Socket? _socket;
    private readonly ImmutableArray<IProtocolFeature> _features;

    public TcpClientProtocolPort(
        TcpClientProtocolPortConfig config, 
        ImmutableArray<IProtocolFeature> features, 
        ImmutableDictionary<string, ParserFactoryDelegate> parsers,
        ImmutableArray<ProtocolInfo> protocols,
        IProtocolCore core) 
        : base(ProtocolHelper.NormalizeId($"{Scheme}_{config.Host}_{config.Port}"), config, features, parsers, protocols, core)
    {
        _config = config;
        _core = core;
        _features = features;
        ArgumentNullException.ThrowIfNull(config);
    }
    public override PortTypeInfo TypeInfo => Info;
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
        InternalAddConnection(new SocketProtocolEndpoint(
            _socket,
            ProtocolHelper.NormalizeId($"{Id}_{_socket.RemoteEndPoint}"),
            _config,InternalCreateParsers(), _features, _core));
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

public static class TcpClientProtocolPortHelper
{
    public static void RegisterTcpClientPort(this IProtocolBuilder builder)
    {
        builder.RegisterPortType(TcpClientProtocolPort.Info, 
            (args, features, parsers,protocols,core) 
                => new TcpClientProtocolPort(TcpClientProtocolPortConfig.Parse(args), features, parsers, protocols, core));
    }
}
