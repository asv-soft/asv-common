using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Asv.IO;

public class TcpServerPipePort:PipePort
{
    public const string IdTagName = "ID"; 
    private readonly TcpPipePortConfig _config;
    private readonly IPipeCore _core;
    private readonly ILogger<TcpServerPipePort> _logger;
    private Thread? _listenTask;
    private Socket? _socket;
    private readonly string _id;
    private const int MaxConnection = 128;

    public TcpServerPipePort(TcpPipePortConfig config, IPipeCore core) 
        : base(config, core)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(core);
        config.Validate();
        _core = core;
        _logger = core.LoggerFactory.CreateLogger<TcpServerPipePort>();
        _config = config;
        _id = $"tcp_s://{config.Host}:{config.Port}";
    }
    public override string Id => _id;

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

    protected override void InternalSafeEnable(CancellationToken startCancel)
    {
        _socket?.Close();
        _socket?.Dispose();
        
        _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        _socket.Bind(new IPEndPoint(IPAddress.Parse(_config.Host), _config.Port));
        _socket.Listen(MaxConnection);
        _listenTask = new Thread(AcceptNewEndpoint) { IsBackground = true, Name = Id };
        _listenTask.Start(startCancel);
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
                    InternalAddPipe(new TcpSocketEndpoint(_config, this, socket, _core));
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

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _socket?.Close();
            _socket?.Dispose();
            _socket = null;
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_socket != null)
        {
            _socket.Close();
            await CastAndDispose(_socket);
            _socket = null;
        }

        await base.DisposeAsyncCore();

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }

    #endregion
    
}

