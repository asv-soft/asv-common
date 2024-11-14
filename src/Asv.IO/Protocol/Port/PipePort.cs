using System;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using R3;
using ZLogger;

namespace Asv.IO;


public class PipePortConfig:PipeEndpointConfig
{
    public int ReconnectTimeoutMs { get; set; } = 5_000;
    public int CheckOldClientsPeriodMs { get; set; } = 3_000;

    public override bool TryValidate(out string? error)
    {
        if (base.TryValidate(out error) == false) return false;
        if (ReconnectTimeoutMs <= 0)
        {
            error = $"{nameof(ReconnectTimeoutMs)} must be greater than 0";
            return false;
        }
        if (CheckOldClientsPeriodMs <= 0)
        {
            error = $"{CheckOldClientsPeriodMs} must be greater than 0";
            return false;
        }
        error = null;
        return true;
    }

    public override string ToString()
    {
        return $"reconnect: {ReconnectTimeoutMs} ms, rotten_check:{CheckOldClientsPeriodMs} ms";
    }
}
public abstract class PipePort:IPipePort
{
    #region Static
    // ReSharper disable once UseCollectionExpression
    // Important !!! (Avoid zero-length array allocations. Use collection expressions) The identity of these arrays matters, so we can't use the shared Array.Empty<T>() instance either explicitly, or indirectly via a collection expression
#pragma warning disable CA1825
    private static readonly IPipeEndpoint[] Disposed = new IPipeEndpoint[0]; 
#pragma warning restore CA1825
    
    public static NameValueCollection ParseQueryString(string requestQueryString)
    {
        var rc = new NameValueCollection();
        var ar1 = requestQueryString.Split('&', '?');
        foreach (var row in ar1)
        {
            if (string.IsNullOrEmpty(row)) continue;
            var index = row.IndexOf('=');
            if (index < 0) continue;
            rc[Uri.UnescapeDataString(row[..index])] = Uri.UnescapeDataString(row[(index + 1)..]); // use Unescape only parts          
        }
        return rc;
    }
    
    public static IPipePort Create(string connectionString, IPipeCore core)
    {
        var uri = new Uri(connectionString);
        IPipePort? result = null;
        if (TcpPipePortConfig.TryParseFromUri(uri, out var tcp))
        {
            Debug.Assert(tcp != null, nameof(tcp) + " != null");
            if (tcp.IsServer)
            {
                result = new TcpServerPipePort(tcp, core);
            }
            else
            {
                result = new TcpClientPipePort(tcp, core);
            }
        }
        else if (UdpPortConfig.TryParseFromUri(uri, out var udp))
        {
            //result = new UdpPort(udp);
        }
        else if (SerialPortConfig.TryParseFromUri(uri, out var ser))
        {
            //result = new CustomSerialPort(ser, timeProvider, logger);
        }
        else
        {
            throw new Exception($"Connection string '{connectionString}' is invalid");
        }
      
        return result;
    }

    #endregion
    
    
    
    private readonly PipePortConfig _config;
    private readonly IPipeCore _core;
    private IPipeEndpoint[] _pipes;
    private CancellationTokenSource? _startStopCancel;
    private readonly ILogger<PipePort> _logger;
    private readonly ReactiveProperty<PipePortStatus> _status = new(PipePortStatus.Disconnected);
    private readonly ReactiveProperty<PipeException?> _error = new(null);
    private readonly ReactiveProperty<bool> _isEnabled = new(false);
    private volatile int _isDisposed;
    private volatile int _isBusy;
    private ITimer? _reconnectTimer;
    private readonly ITimer _timer;
    private readonly Subject<IPipeEndpoint[]> _endpoints = new();

    protected PipePort(PipePortConfig config, IPipeCore core)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(core);
        _config = config;
        _core = core;
        _logger = core.LoggerFactory.CreateLogger<PipePort>();
        _pipes = [];
        _logger.ZLogInformation($"Create port {this} {config}");
        _timer = core.TimeProvider.CreateTimer(RemoveDisposedEndpoints, null, TimeSpan.FromMilliseconds(_config.CheckOldClientsPeriodMs), TimeSpan.FromSeconds(_config.CheckOldClientsPeriodMs));
    }

    private void RemoveDisposedEndpoints(object? state)
    {
        var itemsToDelete = Pipes.Where(x => x.IsDisposed).ToImmutableArray();
        foreach (var item in itemsToDelete)
        {
            InternalRemovePipe(item);
        }
    }

    public abstract string Id { get; }
    public ReadOnlyReactiveProperty<PipeException?> Error => _error;
    public ReadOnlyReactiveProperty<PipePortStatus> Status => _status;
    public ReadOnlyReactiveProperty<bool> IsEnabled => _isEnabled;
    public TagList Tags { get; } = [];
    public IPipeEndpoint[] Pipes => Volatile.Read(ref _pipes);

    public Observable<IPipeEndpoint[]> OnEndpointsChanged => _endpoints;

    protected void InternalAddPipe(IPipeEndpoint pipe)
    {
        if (IsDisposed) return;
        _logger.ZLogInformation($"{this} add pipe endpoint {pipe}");
        for (;;)
        {
            var pipes = Volatile.Read(ref _pipes);
            if (pipes == Disposed) break;
            var count = pipes.Length;
            var newPipe = new IPipeEndpoint[count + 1];
            Array.Copy(pipes, 0, newPipe, 0, count);
            newPipe[count] = pipe;
            if (Interlocked.CompareExchange(ref _pipes, newPipe, pipes) == pipes)
            {
                break;
            }
        }
        
        
    }
    protected void InternalRemovePipe(IPipeEndpoint pipe)
    {
        if (IsDisposed) return;
        _logger.ZLogInformation($"{this} remove pipe endpoint {pipe}");
        try
        {
            for (;;)
            {
                var pipes = Volatile.Read(ref _pipes);
                if (pipes == Disposed) break;
                var count = pipes.Length;
                if (count == 0) break;
                var newPipe = new IPipeEndpoint[count - 1];
                for (var i = 0; i < count; i++)
                {
                    if (pipes[i] == pipe)
                    {
                        Array.Copy(pipes, i + 1, newPipe, i + 1, count - i - 1);
                        Array.Copy(pipes, 0, newPipe, i + 1, count - i - 1);
                        break;
                    }
                }
                if (Interlocked.CompareExchange(ref _pipes, newPipe, pipes) == pipes)
                {
                    break;
                }
                else
                {
                    Debug.Assert( false,"Remove pipe endpoint failed");
                }
            }
            pipe.Dispose();
        }
        catch (Exception e)
        {
            _logger.ZLogError($"Error on dispose pipe {pipe}: {e.Message}");
        }
        
    }
    public void Enable()
    {
        if (IsDisposed) return;
        if (Interlocked.CompareExchange(ref _isBusy, 1, 0) == 1)
        {
            _logger.ZLogTrace($"{this} skip duplicate enable");
            return;
        }
        _logger.ZLogInformation($"Enable {this} port");
        _isEnabled.OnNext(true);
        try
        {
            _status.OnNext(PipePortStatus.InProgress);
            _startStopCancel?.Cancel(false);
            _startStopCancel?.Dispose();
            _startStopCancel = new CancellationTokenSource();
            InternalSafeEnable(_startStopCancel.Token);
            _status.OnNext(PipePortStatus.Connected);
        }
        catch (Exception e)
        {
            InternalPublishError(e);
        }
        finally
        {
            Interlocked.Exchange(ref _isBusy, 0);
        }
    }

    public void Disable()
    {
        if (IsDisposed) return;
        if (Interlocked.CompareExchange(ref _isBusy, 1, 0) == 1)
        {
            _logger.ZLogTrace($"{this} Skip duplicate disable");
            return;
        }
        _logger.ZLogInformation($"Disable {this} port");
        _isEnabled.OnNext(false);
        try
        {
            _status.OnNext(PipePortStatus.InProgress);
            if (_startStopCancel != null)
            {
                var cancel = _startStopCancel;
                if (cancel.IsCancellationRequested == false) cancel.Cancel(false);
                cancel.Dispose();
                _startStopCancel = null;
            }
            InternalSafeDisable();
            _status.OnNext(PipePortStatus.Disconnected);
        }
        catch (Exception e)
        {
            InternalPublishError(e);     
        }   
        finally
        {
            Interlocked.Exchange(ref _isBusy, 0);
        }
    }
    
    protected abstract void InternalSafeDisable();
    protected abstract void InternalSafeEnable(CancellationToken token);
    
    protected void InternalPublishError(Exception ex)
    {
        if (IsDisposed) return;
        _logger.ZLogError(ex,$"Port '{this}' error occured. Reconnect after {_config.ReconnectTimeoutMs} ms. Error message:{ex.Message}");
        _error.OnNext(new PipePortException($"Port {this} error:{ex.Message}",ex,this));
        _status.OnNext(PipePortStatus.Error);
        _reconnectTimer = _core.TimeProvider.CreateTimer(ReconnectAfterError, null, TimeSpan.FromMilliseconds(_config.ReconnectTimeoutMs),
            Timeout.InfiniteTimeSpan);
    }

    private void ReconnectAfterError(object? state)
    {
        _reconnectTimer?.Dispose();
        _reconnectTimer = null;
        if (IsEnabled.CurrentValue == false) return;
        Enable();
    }

    public override string ToString() => Id;

    #region Dispose
    public bool IsDisposed => _isDisposed != 0;
    
    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
        {
            _logger.ZLogTrace($"Skip duplicate dispose call {this}");
        }
        if (disposing)
        {
            _timer.Dispose();
            _endpoints.Dispose();
            _startStopCancel?.Cancel(false);
            _startStopCancel?.Dispose();
            _startStopCancel = null;
            _status.Dispose();
            _error.Dispose();
            _isEnabled.Dispose();
            _reconnectTimer?.Dispose();
            Interlocked.Exchange(ref _pipes, Disposed).ForEach(x=>x.Dispose());
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
        {
            _logger.ZLogTrace($"Skip duplicate dispose call {this}");
        }
        if (_startStopCancel != null)
        {
            _startStopCancel.Cancel(false);
            await CastAndDispose(_startStopCancel);
            _startStopCancel = null;
        }
        await _timer.DisposeAsync();
        await CastAndDispose(_endpoints);
        await CastAndDispose(_status);
        await CastAndDispose(_error);
        await CastAndDispose(_isEnabled);
        if (_reconnectTimer != null) await _reconnectTimer.DisposeAsync();

        foreach (var endpoint in Interlocked.Exchange(ref _pipes, Disposed))
        {
            await CastAndDispose(endpoint);
        }
        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }
    #endregion
}