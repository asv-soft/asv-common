using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Asv.IO;

public class TcpServerPipePortConfig
{
    public int CheckOldClientsPeriodMs { get; set; } = 3_000;
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 7341;
}

public class TcpServerPipePort:PipePort
{
    private readonly TcpServerPipePortConfig _config;
    private readonly IPipeCore _core;
    private readonly ITimer _timer;
    private TcpListener? _tcp;
    private readonly ILogger<TcpServerPipePort> _logger;
    private Thread? _listenTask;
    private CancellationTokenSource? _listenCancel;

    public TcpServerPipePort(TcpServerPipePortConfig config, IPipeCore core) : base(core)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(core);
        _core = core;
        _logger = core.LoggerFactory.CreateLogger<TcpServerPipePort>();
        _config = config;
        _timer = core.TimeProvider.CreateTimer(RemoveOldEndpoints, null, TimeSpan.FromMilliseconds(_config.CheckOldClientsPeriodMs), TimeSpan.FromSeconds(_config.CheckOldClientsPeriodMs));
    }

    private void RemoveOldEndpoints(object? state)
    {
        
    }

    protected void InternalStop()
    {
        if (_tcp != null)
        {
            var tcp = _tcp;
            tcp.Stop();
            tcp.Dispose();
            tcp.Stop();    
            _tcp = null;
        }

        if (_listenCancel != null)
        {
            var cancel = _listenCancel;
            if (cancel.IsCancellationRequested == false) cancel.Cancel(false);
            cancel.Dispose();
            _listenCancel = null;
        }
        
    }

    protected void InternalStart()
    {
        _tcp?.Stop();
        _tcp?.Dispose();
        _tcp = new TcpListener(IPAddress.Parse(_config.Host), _config.Port);
        _tcp.Start();
        _listenCancel = new CancellationTokenSource();
        _listenTask = new Thread(AcceptNewEndpoint);
        _listenTask.Start(_listenCancel.Token);
    }

    private async void AcceptNewEndpoint(object? state)
    {
        var cancel = (CancellationToken)(state ?? throw new ArgumentNullException(nameof(state)));
        var tcp = _tcp;
        Debug.Assert(tcp != null);
        try
        {
            while (cancel is { IsCancellationRequested: false })
            {
                try
                {
                    var newClient = await tcp.AcceptTcpClientAsync(cancel);
                    InternalAddPipe(new TcpServerEndpoint(newClient, _core));
                }
                catch (ThreadAbortException ex)
                {
                    _logger.ZLogDebug(ex, $"Thread abort exception:{ex.Message}");
                    // ignore
                }
                catch (SocketException ex)
                {
                    _logger.ZLogDebug(ex, $"Socket exception:{ex.Message}");
                    // ignore
                }
                catch (Exception ex)
                {
                    _logger.ZLogError(ex, $"Unhandled exception:{ex.Message}");
                    Debug.Assert(false);
                    // ignore
                }
            }
        }
        catch (ThreadAbortException ex)
        {
            _logger.ZLogDebug(ex, $"Thread abort exception:{ex.Message}");
            // ignore
        }
    }

}

internal class TcpServerEndpoint : PipeEndpoint
{
    private readonly TcpClient _newClient;

    public TcpServerEndpoint(TcpClient newClient, IPipeCore core)
    : base(core)
    {
        _newClient = newClient;
    }

    protected override Task InternalWrite(PipeReader outputReader, CancellationToken cancel)
    {
        return outputReader.CopyToAsync(_newClient.GetStream(),cancel);
    }

    protected override Task InternalRead(PipeWriter inputWriter, CancellationToken cancel)
    {
        return _newClient.GetStream().CopyToAsync(inputWriter, cancel);
    }
}