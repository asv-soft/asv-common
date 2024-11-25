using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Asv.IO;

public class TcpClientProtocolPortConfig(Uri connectionString) : ProtocolPortConfig(connectionString)
{
   
}

public class TcpClientProtocolPort:ProtocolPort<TcpClientProtocolPortConfig>
{
    public const string Scheme = "tcp";
    public static readonly PortTypeInfo Info  = new(Scheme, "Tcp client port");
    
    private readonly TcpClientProtocolPortConfig _config;
    private readonly IProtocolContext _context;
    private Socket? _socket;
    private readonly IPEndPoint _remoteEndpoint;

    public TcpClientProtocolPort(
        TcpClientProtocolPortConfig config, 
        IProtocolContext context,
        IStatisticHandler statistic) 
        : base(ProtocolHelper.NormalizeId($"{Scheme}_{config.Host}_{config.Port}"), config, context,statistic)
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
        _socket.Connect(_remoteEndpoint);
        _socket.SendBufferSize = _config.SendBufferSize;
        _socket.SendTimeout = _config.SendTimeout;
        _socket.ReceiveBufferSize = _config.ReadBufferSize;
        _socket.ReceiveTimeout = _config.ReadTimeout;
        InternalAddConnection(new SocketProtocolEndpoint(
            _socket,
            ProtocolHelper.NormalizeId($"{Id}_{_socket.RemoteEndPoint}"),
            _config,InternalCreateParsers(), _context,StatisticHandler));
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
            (cs,  context,stat) 
                => new TcpClientProtocolPort(new TcpClientProtocolPortConfig(cs), context,stat));
    }
}
