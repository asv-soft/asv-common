using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Asv.IO;

public class UdpProtocolPortConfig(Uri connectionString) : ProtocolPortConfig(connectionString)
{
    public static UdpProtocolPortConfig CreateDefault() => new(new Uri($"{UdpProtocolPort.Scheme}://127.0.0.1:8080"));

    public const string RemoteHostKey = "rhost";
    public const string RemotePortKey = "rport";
    public const string RemoteEndpoints = "remote";
    
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
    
    public IEnumerable<IPEndPoint> GetRemoteEndpoints()
    {
        var obsoleteSettings = GetRemoteEndpoint();
        if (obsoleteSettings != null)
        {
            yield return obsoleteSettings;
        }
        var endpoints = Query.GetValues(RemoteEndpoints);
        if (endpoints == null || endpoints.Length == 0)
        {
            yield break;
        }

        foreach (var endpoint in endpoints)
        {
            if (IPEndPoint.TryParse(endpoint, out var ep))
            {
                yield return ep;
            }
        }
    }

    public override object Clone() => new UdpProtocolPortConfig(AsUri());
   
}

public class UdpProtocolPort:ProtocolPort<UdpProtocolPortConfig>
{
    public const string Scheme = "udp";
    public static PortTypeInfo Info => new(Scheme, "Udp protocol port");
    private readonly UdpProtocolPortConfig _config;
    private readonly IProtocolContext _context;
    private readonly IPEndPoint _localEndPoint;
    private readonly ImmutableHashSet<IPEndPoint> _remoteEndPoints;
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

        _localEndPoint = config.CheckAndGetLocalHost();
        _remoteEndPoints = config.GetRemoteEndpoints().ToImmutableHashSet();
        
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
        _socket = new Socket(_localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(_localEndPoint);
        // if we have list of remote endpoints, we should create it for sending data
        foreach (var recvAddr in _remoteEndPoints)
        {
            InternalAddEndpoint(new UdpSocketProtocolEndpoint(
                _socket,
                recvAddr,
                ProtocolHelper.NormalizeId($"{Id}_{recvAddr}"),
                _config, InternalCreateParsers(), _context, StatisticHandler));    
        }
        Task.Factory.StartNew(BeginAcceptNewData, token, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach).SafeFireAndForget();
    }
    
    private void BeginAcceptNewData(object? state)
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var cancel = (CancellationToken) state!;
        var buffer = ArrayPool<byte>.Shared.Rent(65536); // max UDP packet size
        try
        {
            
            while (_socket != null && cancel is { IsCancellationRequested: false })
            {
                // receive data from any remote endpoint
                EndPoint recvAddr = new IPEndPoint(IPAddress.Any, 0);
                var readSize = _socket.ReceiveFrom(buffer, ref recvAddr);
                if (recvAddr is not IPEndPoint recvAddrIPEndPoint)
                {
                    _logger.ZLogWarning($"Received data from unsupported endpoint type: {recvAddr.GetType().Name}. Data will be ignored.");
                    continue;
                }
                // if we have whitelist of remote endpoints, we should check if received data is from one of them
                if (_remoteEndPoints.Count > 0 && !_remoteEndPoints.Contains(recvAddrIPEndPoint))
                {
                    continue;
                }
                var exist = (UdpSocketProtocolEndpoint?)Endpoints.FirstOrDefault(x => recvAddrIPEndPoint.Equals(((UdpSocketProtocolEndpoint)x).RemoteEndPoint));
                // if we have no endpoint for this remote address, we should create it
                if (exist == null)
                {
                    InternalAddEndpoint(exist = new UdpSocketProtocolEndpoint(
                        _socket,
                        recvAddrIPEndPoint,
                        ProtocolHelper.NormalizeId($"{Id}_{recvAddr}"),
                        _config, InternalCreateParsers(), _context, StatisticHandler));    
                }
                // this method blocks this thread until data is processed by endpoint its own read thread
                exist.ApplyData(buffer, readSize);
            }
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
        {
            // graceful shutdown: exit the loop without treating it as an error
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
        {
            // Windows: overlapped I/O cancelled (CancelIoEx); also expected during shutdown
        }
        catch (ObjectDisposedException)
        {
            // socket already closed — expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.ZLogDebug(ex, $"Unhandled exception on {nameof(BeginAcceptNewData)}:{ex.Message}");
            InternalRisePortErrorAndReconnect(ex);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

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
}

public static class UdpProtocolPortHelper
{
    public static IProtocolPort AddUdpPort(this IProtocolRouter src, Action<UdpProtocolPortConfig> edit)
    {
        var cfg = UdpProtocolPortConfig.CreateDefault();
        edit(cfg);
        return src.AddPort(cfg.AsUri());
    }
    public static void RegisterUdpPort(this IProtocolPortBuilder builder)
    {
        builder.Register(UdpProtocolPort.Info, 
            (cs, context,stat) 
                => new UdpProtocolPort(new UdpProtocolPortConfig(cs), context,stat));
    }
}

