using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Asv.IO;

public class TcpServerProtocolPortConfig(Uri connectionString)
    : ProtocolPortConfig(connectionString)
{
    public static TcpServerProtocolPortConfig CreateDefault() =>
        new(new Uri($"{TcpServerProtocolPort.Scheme}://127.0.0.1:7341"));

    public const string MaxConnectionKey = "maxConnection";
    public int? MaxConnection
    {
        get
        {
            var maxConnection = Query.Get(MaxConnectionKey);
            if (
                string.IsNullOrWhiteSpace(maxConnection)
                || !int.TryParse(maxConnection, out var port)
            )
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

    public override object Clone() => new TcpServerProtocolPortConfig(AsUri());
}

public class TcpServerProtocolPort : ProtocolPort<TcpServerProtocolPortConfig>
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
        IStatisticHandler statistic
    )
        : base(
            ProtocolHelper.NormalizeId($"{Scheme}_{config.Host}_{config.Port}"),
            config,
            context,
            false,
            statistic
        )
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(context);
        _config = config;
        _context = context;
        _logger = context.LoggerFactory.CreateLogger<TcpServerProtocolPort>();
        _bindEndpoint = config.CheckAndGetLocalHost();
    }

    public override PortTypeInfo TypeInfo => Info;
    internal EndPoint? LocalEndPoint => _socket?.LocalEndPoint;

    protected override void InternalSafeEnable(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        _socket?.Close();
        _socket?.Dispose();

        var listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
        _socket = listener;
        token.ThrowIfCancellationRequested();
        listener.Bind(_bindEndpoint);
        if (_config.MaxConnection == null)
        {
            listener.Listen();
        }
        else
        {
            listener.Listen(_config.MaxConnection.Value);
        }
        token.ThrowIfCancellationRequested();
        Task.Factory.StartNew(
            () => AcceptNewEndpoint(listener, token),
            token,
            TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
            TaskScheduler.Default
        );
    }

    private void AcceptNewEndpoint(Socket listener, CancellationToken cancel)
    {
        try
        {
            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    var socket = listener.Accept();
                    try
                    {
                        cancel.ThrowIfCancellationRequested();
                        InternalAddEndpoint(
                            new TcpServerSocketProtocolEndpoint(
                                socket,
                                ProtocolHelper.NormalizeId($"{Id}_{socket.RemoteEndPoint}"),
                                _config,
                                InternalCreateParsers(),
                                _context,
                                StatisticHandler
                            ),
                            cancel
                        );
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
                {
                    // graceful shutdown: exit the loop without treating it as an error
                    return;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
                {
                    // Windows: overlapped I/O cancelled (CancelIoEx); also expected during shutdown
                    return;
                }
                catch (ObjectDisposedException)
                {
                    // socket already closed — expected during shutdown
                    return;
                }
                catch (OperationCanceledException) when (cancel.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception) when (cancel.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.ZLogError(ex, $"Unhandled exception:{ex.Message}");
                    Debug.Assert(false, "Unexpected protocol or transport failure.");
                    InternalRisePortErrorAndReconnect(ex);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogDebug(
                ex,
                $"Unhandled exception on {nameof(AcceptNewEndpoint)}:{ex.Message}"
            );
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
    public static IProtocolPort AddTcpServerPort(
        this IProtocolRouter src,
        Action<TcpServerProtocolPortConfig> edit
    )
    {
        var cfg = TcpServerProtocolPortConfig.CreateDefault();
        edit(cfg);
        return src.AddPort(cfg.AsUri());
    }

    public static void RegisterTcpServerPort(this IProtocolPortBuilder builder)
    {
        builder.Register(
            TcpServerProtocolPort.Info,
            (cs, context, stat) =>
                new TcpServerProtocolPort(new TcpServerProtocolPortConfig(cs), context, stat)
        );
    }
}
