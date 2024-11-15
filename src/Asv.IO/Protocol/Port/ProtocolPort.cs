using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using R3;
using ZLogger;

namespace Asv.IO;

public class ProtocolPortConfig : ProtocolConnectionConfig
{
    public int ReconnectTimeoutMs { get; set; } = 5_000;
}

public abstract class ProtocolPort : IProtocolPort
{
    

    private readonly ProtocolPortConfig _config;
    private readonly IPipeCore _core;
    private readonly ILogger<ProtocolPort> _logger;
    private readonly ObservableList<IProtocolConnection> _connections = new();
    private readonly ReaderWriterLockSlim _connectionsLock = new();
    private readonly Subject<IProtocolMessage> _onMessageReceived = new();
    private readonly Subject<IProtocolMessage> _onMessageSent = new();
    private int _isBusy;
    private readonly ReactiveProperty<ProtocolException?> _error = new();
    private readonly ReactiveProperty<ProtocolPortStatus> _status = new();
    private readonly ReactiveProperty<bool> _isEnabled = new();
    private CancellationTokenSource? _startStopCancel;
    private ITimer? _reconnectTimer;
    private int _isDisposed;

    protected ProtocolPort(string id, ProtocolPortConfig config, IPipeCore core)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(id));
        Id = id;
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(core);
        _config = config;
        _core = core;
        _logger = core.LoggerFactory.CreateLogger<ProtocolPort>();
        Tags = [];
        _logger.ZLogInformation($"Create port {this} {config}");
    }
    
    protected void InternalRemoveConnection(IProtocolConnection connection)
    {
        if (IsDisposed) return;
        _logger.ZLogInformation($"{this} remove connection {connection}");
        _connectionsLock.EnterWriteLock();
        try
        {
            _connections.Remove(connection);
            connection.Dispose();
        }
        catch (Exception e)
        {
            _logger.ZLogError($"Error on dispose connection {connection}: {e.Message}");
            Debug.Fail("Error on dispose connection");
        }
        finally
        {
            _connectionsLock.ExitWriteLock();
        }
        
    }
    
    protected void InternalAddConnection(IProtocolConnection pipe)
    {
        if (IsDisposed) return;
        _logger.ZLogInformation($"{this} add pipe endpoint {pipe}");
        _connectionsLock.EnterWriteLock();
        try
        {
            _connections.Add(pipe);
            // we no need to dispose subscriptions here, because it will be disposed by connection itself
            pipe.IsConnected.Where(x => x == false).Subscribe(pipe, (x, p) => InternalRemoveConnection(p));
            pipe.OnMessageReceived.Subscribe(_onMessageReceived.AsObserver());
        }
        catch (Exception e)
        {
            _logger.ZLogError($"Error on dispose pipe {pipe}: {e.Message}");
            Debug.Fail("Error on dispose pipe");
        }
        finally
        {
            _connectionsLock.ExitWriteLock();
        }
    }
    
    public string Id { get; }

    public ReadOnlyReactiveProperty<ProtocolException?> Error => _error;

    public ReadOnlyReactiveProperty<ProtocolPortStatus> Status => _status;

    public ReadOnlyReactiveProperty<bool> IsEnabled => _isEnabled;

    public TagList Tags { get; } 
    public IReadOnlyObservableList<IProtocolConnection> Connections => _connections;
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
            _status.OnNext(ProtocolPortStatus.InProgress);
            _startStopCancel?.Cancel(false);
            _startStopCancel?.Dispose();
            _startStopCancel = new CancellationTokenSource();
            InternalSafeEnable(_startStopCancel.Token);
            _status.OnNext(ProtocolPortStatus.Connected);
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
            _status.OnNext(ProtocolPortStatus.InProgress);
            if (_startStopCancel != null)
            {
                var cancel = _startStopCancel;
                if (cancel.IsCancellationRequested == false) cancel.Cancel(false);
                cancel.Dispose();
                _startStopCancel = null;
            }
            InternalSafeDisable();
            _status.OnNext(ProtocolPortStatus.Disconnected);
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

    protected void InternalPublishError(Exception ex)
    {
        if (IsDisposed) return;
        _logger.ZLogError(ex,$"Port '{this}' error occured. Reconnect after {_config.ReconnectTimeoutMs} ms. Error message:{ex.Message}");
        _error.OnNext(new ProtocolPortException(this,$"Port {this} error:{ex.Message}",ex));
        _status.OnNext(ProtocolPortStatus.Error);
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
    
    protected abstract void InternalSafeDisable();
    protected abstract void InternalSafeEnable(CancellationToken token);

    public Observable<IProtocolMessage> OnMessageReceived => _onMessageReceived;
    public Observable<IProtocolMessage> OnMessageSent => _onMessageSent;

    public async ValueTask Send(IProtocolMessage message, CancellationToken cancel = default)
    {
        _connectionsLock.EnterReadLock();
        try
        {
            foreach (var connection in _connections)
            {
                await connection.Send(message, cancel);
            }
        }
        finally
        {
            _connectionsLock.EnterReadLock();   
        }
    }
    
    #region Dispose

    public bool IsDisposed => _isDisposed != 0;
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
            {
                _logger.ZLogTrace($"Skip duplicate {nameof(Dispose)} call {this}");
            }
            _connectionsLock.EnterWriteLock();
            try
            {
                foreach (var connection in _connections)
                {
                    connection.Dispose();
                }
                _connections.Clear();
            }
            finally
            {
                _connectionsLock.ExitWriteLock();
            }
            _connectionsLock.Dispose();
            _onMessageReceived.Dispose();
            _onMessageSent.Dispose();
            _error.Dispose();
            _status.Dispose();
            _isEnabled.Dispose();
            _startStopCancel?.Dispose();
            _reconnectTimer?.Dispose();
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
        {
            _logger.ZLogTrace($"Skip duplicate {nameof(DisposeAsync)} {Id}");
            return;
        }
        _logger.ZLogTrace($"{nameof(DisposeAsync)} {Id}");
        
        _connectionsLock.EnterWriteLock();
        try
        {
            foreach (var connection in _connections)
            {
                await connection.DisposeAsync();
            }
            _connections.Clear();
        }
        finally
        {
            _connectionsLock.ExitWriteLock();
        }
        await CastAndDispose(_connectionsLock);
        await CastAndDispose(_onMessageReceived);
        await CastAndDispose(_onMessageSent);
        await CastAndDispose(_error);
        await CastAndDispose(_status);
        await CastAndDispose(_isEnabled);
        if (_startStopCancel != null) await CastAndDispose(_startStopCancel);
        if (_reconnectTimer != null) await _reconnectTimer.DisposeAsync();

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    #endregion
}