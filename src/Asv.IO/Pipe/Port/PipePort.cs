using System;
using System.Collections.Immutable;
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
    private readonly PipePortConfig _config;
    private readonly IPipeCore _core;
    private readonly ObservableList<IPipeEndpoint> _pipes;
    private CancellationTokenSource? _startStopCancel;
    private readonly ILogger<PipePort> _logger;
    private readonly ReactiveProperty<PipePortStatus> _status = new(PipePortStatus.Disconnected);
    private readonly ReactiveProperty<PipeException?> _error = new(null);
    private readonly ReactiveProperty<bool> _isEnabled = new(false);
    private volatile int _isDisposed;
    private volatile int _isBusy;
    private ITimer? _reconnectTimer;
    private readonly ITimer _timer;

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
    public IReadOnlyObservableList<IPipeEndpoint> Pipes => _pipes;
    protected void InternalAddPipe(IPipeEndpoint pipe)
    {
        if (IsDisposed) return;
        _logger.ZLogInformation($"{this} add pipe endpoint {pipe}");
        _pipes.Add(pipe);
    }
    protected void InternalRemovePipe(IPipeEndpoint pipe)
    {
        if (IsDisposed) return;
        _logger.ZLogInformation($"{this} remove pipe endpoint {pipe}");
        try
        {
            _pipes.Remove(pipe);
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
            _startStopCancel?.Cancel(false);
            _startStopCancel?.Dispose();
            _startStopCancel = null;
            _status.Dispose();
            _error.Dispose();
            _isEnabled.Dispose();
            _reconnectTimer?.Dispose();
            _pipes.ToImmutableArray().ForEach(x=>x.Dispose());
            _pipes.Clear();
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
        await CastAndDispose(_status);
        await CastAndDispose(_error);
        await CastAndDispose(_isEnabled);
        if (_reconnectTimer != null) await _reconnectTimer.DisposeAsync();

        foreach (var endpoint in _pipes.ToImmutableArray())
        {
            await CastAndDispose(endpoint);
        }
        _pipes.Clear();
        
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