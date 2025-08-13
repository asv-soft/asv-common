using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;
using Timeout = System.Threading.Timeout;

namespace Asv.IO;



public abstract class ProtocolPort<TConfig> : ProtocolConnection, IProtocolPort
    where TConfig:ProtocolPortConfig
{
    private volatile int _isEnableBusy;
    private volatile int _isDisableBusy;
    private readonly ImmutableArray<ProtocolInfo> _protocols;
    private readonly IProtocolContext _context;
    private readonly bool _reconnectWhenEndpointError;
    private readonly ILogger _logger;
    private ImmutableArray<IProtocolEndpoint> _endpoints = [];
    private readonly ReactiveProperty<ProtocolException?> _error = new();
    private readonly ReactiveProperty<ProtocolPortStatus> _status = new();
    private readonly ReactiveProperty<bool> _isEnabled = new();
    private CancellationTokenSource? _startStopCancel;
    private ITimer? _reconnectTimer;
    private readonly Subject<IProtocolEndpoint> _endpointAdded = new();
    private readonly Subject<IProtocolEndpoint> _endpointRemoved = new();
    private readonly TConfig _config;
    

    protected ProtocolPort(string id,
        TConfig config,
        IProtocolContext context, bool reconnectWhenEndpointError,
        IStatisticHandler? statistic = null):base(id, context, statistic)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(id));
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
        _reconnectWhenEndpointError = reconnectWhenEndpointError;
        _config = config;
        if (config.EnabledProtocols == null)
        {
            _protocols = context.AvailableProtocols;
        }
        else
        {
            var hash = new HashSet<string>(config.EnabledProtocols);
            _protocols = [..Context.AvailableProtocols.Where(x => hash.Contains(x.Id))];
        }
        
        foreach (var protocol in _protocols)
        {
            if (context.ParserFactory.ContainsKey(protocol.Id) == false)
                throw new ArgumentException($"Parser for protocol {protocol} not found");
        }
        _logger = context.LoggerFactory.CreateLogger<ProtocolPort<TConfig>>();
        _logger.ZLogInformation($"Create port {this} with config {config}");
        
    }

    private void ClearAndDisposeAllEndpoints()
    {
        if (_endpoints.Length == 0) return;
        _logger.ZLogTrace($"{this} clear all endpoints '{_endpoints.Length}'");
        ImmutableArray<IProtocolEndpoint> after, before;
        do
        {
            before = _endpoints;
            after = [];    
        }
        // check if the value is changed by another thread while we are removing the endpoint
        while (ImmutableInterlocked.InterlockedCompareExchange(ref _endpoints, after, before) != before);
        foreach (var endpoint in before)
        {
            try
            {
                endpoint.Dispose();
                _endpointRemoved.OnNext(endpoint);
            }
            catch (Exception e)
            {
                _logger.ZLogError($"Error on dispose endpoint {endpoint}: {e.Message}");
                Debug.Fail("Error on dispose endpoint");
            }
        }
    }

    protected void RemoveAndDisposeEndpoint(IProtocolEndpoint endpoint, ProtocolConnectionException? err)
    {
        if (IsDisposed) return;
        _logger.ZLogInformation($"{this} remove endpoint {endpoint}");
        ImmutableArray<IProtocolEndpoint> after, before;
        do
        {
            before = _endpoints;
            after = before.Remove(endpoint);    
        }
        // check if the value is changed by another thread while we are removing the endpoint
        while (ImmutableInterlocked.InterlockedCompareExchange(ref _endpoints, after, before) != before);
        
        try
        {
            endpoint.Dispose();
        }
        catch (Exception e)
        {
            _logger.ZLogError($"Error on dispose endpoint {endpoint}: {e.Message}");
            Debug.Fail("Error on dispose endpoint");
        }
        _endpointRemoved.OnNext(endpoint);
        if (err != null && _reconnectWhenEndpointError)
        {
            InternalRisePortErrorAndReconnect(err);
        }
    }
    
    protected void InternalAddEndpoint(IProtocolEndpoint endpoint)
    {
        if (IsDisposed) return;
        _logger.ZLogInformation($"{this} add endpoint {endpoint}");
        ImmutableArray<IProtocolEndpoint> after, before;
        do
        {
            before = _endpoints;
            after = before.Add(endpoint);    
        }
        // check if the value is changed by another thread while we are adding the endpoint
        while (ImmutableInterlocked.InterlockedCompareExchange(ref _endpoints, after, before) != before);
        
        // we don't need to dispose subscriptions here, because it will be complete by endpoint itself
        endpoint.LastError.Where(x => x != null).Subscribe(endpoint, (x, p) => RemoveAndDisposeEndpoint(p,x));
        endpoint.OnRxMessage.Subscribe(InternalPublishRxMessage,InternalPublishRxError, _ => { });
        _endpointAdded.OnNext(endpoint);
    }
    protected ImmutableArray<IProtocolParser> InternalCreateParsers()
    {
        return [.._protocols.Select(x => Context.ParserFactory[x.Id](_context, StatisticHandler))];
    }

    public ProtocolPortConfig Config => (ProtocolPortConfig)_config.Clone();
    public abstract PortTypeInfo TypeInfo { get; }
    public IEnumerable<ProtocolInfo> EnabledProtocols => _protocols;
    public ReadOnlyReactiveProperty<ProtocolException?> Error => _error;
    public ReadOnlyReactiveProperty<ProtocolPortStatus> Status => _status;
    public ReadOnlyReactiveProperty<bool> IsEnabled => _isEnabled;
    public ImmutableArray<IProtocolEndpoint> Endpoints => _endpoints;
    public Observable<IProtocolEndpoint> EndpointAdded => _endpointAdded;
    public Observable<IProtocolEndpoint> EndpointRemoved => _endpointRemoved;

    public void Enable()
    {
        if (IsEnabled.CurrentValue)
        {
            _logger.ZLogWarning($"Port {this} already enabled, skip enable request");
        }
        _logger.ZLogInformation($"Enable {this} port");
        _config.IsEnabled = true;
        _isEnabled.Value = true;
        Task.Factory.StartNew(InternalEnable).SafeFireAndForget();
    }

    private void InternalEnable()
    {
        if (IsDisposed) return;
        if (Interlocked.CompareExchange(ref _isEnableBusy, 1, 0) == 1)
        {
            _logger.ZLogTrace($"Port {this} skip duplicate enable ");
            return;
        }
        try
        {
            _status.OnNext(ProtocolPortStatus.InProgress);
            _startStopCancel?.Cancel(false);
            _startStopCancel?.Dispose();
            _startStopCancel = new CancellationTokenSource();
            InternalSafeDisable();
            ClearAndDisposeAllEndpoints();
            InternalSafeEnable(_startStopCancel.Token);
            _status.OnNext(ProtocolPortStatus.Connected);
        }
        catch (Exception e)
        {
            InternalRisePortErrorAndReconnect(e);
        }
        finally
        {
            Interlocked.Exchange(ref _isEnableBusy, 0);
        }
    }

    public void Disable()
    {
        if (IsEnabled.CurrentValue == false)
        {
            _logger.ZLogWarning($"Port {this} already disabled, skip disable request");
        }
        _config.IsEnabled = false;
        _isEnabled.Value = false;
        Task.Factory.StartNew(InternalDisable).SafeFireAndForget();
    }
    
    private void InternalDisable()
    {
        if (IsDisposed) return;
        if (Interlocked.CompareExchange(ref _isDisableBusy, 1, 0) == 1)
        {
            _logger.ZLogTrace($"{this} Skip duplicate disable");
            return;
        }
        _logger.ZLogInformation($"Disable {this} port");
        
        
        try
        {
            _status.Value = ProtocolPortStatus.InProgress;
            if (_startStopCancel != null)
            {
                var cancel = _startStopCancel;
                if (cancel.IsCancellationRequested == false)
                {
                    cancel.Cancel(false);
                }
                
                cancel.Dispose();
                _startStopCancel = null;
            }
            InternalSafeDisable();
            ClearAndDisposeAllEndpoints();
            _status.Value = ProtocolPortStatus.Disconnected;
        }
        catch (Exception e)
        {
            InternalRisePortErrorAndReconnect(e);     
        }   
        finally
        {
            Interlocked.Exchange(ref _isDisableBusy, 0);
        }
    }

    protected void InternalRisePortErrorAndReconnect(Exception ex)
    {
        if (IsDisposed) return;
        if (_reconnectTimer != null)
        {
            _reconnectTimer.Dispose();
            _reconnectTimer = null;
        }
        ClearAndDisposeAllEndpoints();
        _logger.ZLogError(ex,$"Port {this} error occured. Reconnect after {_config.ReconnectTimeoutMs} ms. Error message:{ex.Message}");
        _error.OnNext(new ProtocolPortException(this,$"Port {this} error:{ex.Message}",ex));
        _status.Value = ProtocolPortStatus.Error;
        _reconnectTimer = _context.TimeProvider.CreateTimer(ReconnectAfterError, null, TimeSpan.FromMilliseconds(_config.ReconnectTimeoutMs),
            Timeout.InfiniteTimeSpan);
    }
    
    private void ReconnectAfterError(object? state)
    {
        _reconnectTimer?.Dispose();
        _reconnectTimer = null;
        if (IsEnabled.CurrentValue == false) return;
        if (_status.CurrentValue == ProtocolPortStatus.Connected) return;
        InternalEnable();
    }
    
    protected abstract void InternalSafeDisable();
    protected abstract void InternalSafeEnable(CancellationToken token);
    public override async ValueTask Send(IProtocolMessage message, CancellationToken cancel = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (IsDisposed)
        {
            return;
        }
        
        cancel.ThrowIfCancellationRequested();
        var newMessage = await InternalFilterTxMessage(message);
        if (newMessage == null) return;
        var endpoints = _endpoints;
        foreach (var endpoint in endpoints)
        {
            await endpoint.Send(newMessage, cancel);
        }
        InternalPublishTxMessage(newMessage);
    }

    public override string ToString()
    {
        return $"[PORT]({Id})";
    }

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClearAndDisposeAllEndpoints();
            _endpointAdded.Dispose();
            _endpointRemoved.Dispose();
            _error.Dispose();
            _status.Dispose();
            _isEnabled.Dispose();
            _startStopCancel?.Dispose();
            _reconnectTimer?.Dispose();
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        _logger.ZLogTrace($"{nameof(DisposeAsync)} {Id}");
        ClearAndDisposeAllEndpoints();
        await CastAndDispose(_endpointAdded);
        await CastAndDispose(_endpointRemoved);
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

    #endregion
}