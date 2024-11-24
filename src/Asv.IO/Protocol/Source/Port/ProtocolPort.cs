using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Asv.Common;
using DotNext.Threading;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using R3;
using ZLogger;
using Timeout = System.Threading.Timeout;

namespace Asv.IO;

public class ProtocolPortConfig : ProtocolEndpointConfig
{
    public int ReconnectTimeoutMs { get; set; } = 5_000;
    public override string ToString()
    {
        return $"{base.ToString()}, ReconnectTimeoutMs:{ReconnectTimeoutMs}";
    }
}

public abstract class ProtocolPort : ProtocolConnection, IProtocolPort
{
    private int _isBusy;
    private readonly ProtocolPortConfig _config;
    private readonly ImmutableDictionary<string, ParserFactoryDelegate> _parsers;
    private readonly ImmutableArray<ProtocolInfo> _protocols;
    private readonly IProtocolContext _context;
    private readonly ILogger<ProtocolPort> _logger;
    private ImmutableArray<IProtocolEndpoint> _endpoints = [];
    private readonly ReactiveProperty<ProtocolException?> _error = new();
    private readonly ReactiveProperty<ProtocolPortStatus> _status = new();
    private readonly ReactiveProperty<bool> _isEnabled = new();
    private CancellationTokenSource? _startStopCancel;
    private ITimer? _reconnectTimer;
    private readonly Subject<IProtocolEndpoint> _endpointAdded = new();
    private readonly Subject<IProtocolEndpoint> _endpointRemoved = new();

    protected ProtocolPort(string id,
        ProtocolPortConfig config,
        ChannelWriter<IProtocolMessage> rxChannel, 
        ChannelWriter<ProtocolException> errorChannel,
        ImmutableDictionary<string, ParserFactoryDelegate> parsers,
        ImmutableArray<ProtocolInfo> protocols,
        IProtocolContext context,
        IStatisticHandler? statistic = null):base(id, features, rxChannel, errorChannel, context,statistic)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(id));
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(context);
        _config = config;
        _parsers = parsers;
        _protocols = protocols;
        _context = context;
        _logger = context.LoggerFactory.CreateLogger<ProtocolPort>();
        _logger.ZLogInformation($"Create port {this} {config}");
    }
    
    protected void InternalRemoveConnection(IProtocolEndpoint endpoint)
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
    }
    
    protected void InternalAddConnection(IProtocolEndpoint endpoint)
    {
        if (IsDisposed) return;
        _logger.ZLogInformation($"{this} add endpoint {endpoint}");
        ImmutableArray<IProtocolEndpoint> after, before;
        do
        {
            before = _endpoints;
            after = before.Add(endpoint);    
        }
        // check if the value is changed by another thread while we are removing the endpoint
        while (ImmutableInterlocked.InterlockedCompareExchange(ref _endpoints, after, before) != before);
        
        // we don't need to dispose subscriptions here, because it will be complete by endpoint itself
        endpoint.IsConnected.Where(x => x == false).Subscribe(endpoint, (x, p) => InternalRemoveConnection(p));
    }
    protected ImmutableArray<IProtocolParser> InternalCreateParsers()
    {
        return [.._protocols.Select(x => _parsers[x.Id](_context, StatisticHandler))];
    }
    public abstract PortTypeInfo TypeInfo { get; }
    public IEnumerable<ProtocolInfo> Protocols => _protocols;
    public ReadOnlyReactiveProperty<ProtocolException?> Error => _error;
    public ReadOnlyReactiveProperty<ProtocolPortStatus> Status => _status;
    public ReadOnlyReactiveProperty<bool> IsEnabled => _isEnabled;
    public ImmutableArray<IProtocolEndpoint> Endpoints => _endpoints;

    public Observable<IProtocolEndpoint> EndpointAdded => _endpointAdded;

    public Observable<IProtocolEndpoint> EndpointRemoved => _endpointRemoved;

    public void Enable()
    {
        if (IsDisposed) return;
        if (Interlocked.CompareExchange(ref _isBusy, 1, 0) == 1)
        {
            _logger.ZLogTrace($"Port {this} skip duplicate enable ");
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
        _logger.ZLogError(ex,$"Port {this} error occured. Reconnect after {_config.ReconnectTimeoutMs} ms. Error message:{ex.Message}");
        _error.OnNext(new ProtocolPortException(this,$"Port {this} error:{ex.Message}",ex));
        _status.OnNext(ProtocolPortStatus.Error);
        _reconnectTimer = _context.TimeProvider.CreateTimer(ReconnectAfterError, null, TimeSpan.FromMilliseconds(_config.ReconnectTimeoutMs),
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
    public override async ValueTask Send(IProtocolMessage message, CancellationToken cancel = default)
    {
        if (IsDisposed) return;
        var newMessage = await InternalFilterTxMessage(message);
        if (newMessage == null) return;
        var endpoints = _endpoints;
        foreach (var connection in endpoints)
        {
            await connection.Send(newMessage, cancel);
        }
    }

    public override string ToString()
    {
        return $"[PORT]({Id})";
    }

    #region Dispose

    protected override async void Dispose(bool disposing)
    {
        if (disposing)
        {
            ImmutableArray<IProtocolEndpoint> after, before;
            do
            {
                before = _endpoints;
                after = [];    
            }
            // check if the value is changed by another thread while we are removing the endpoint
            while (ImmutableInterlocked.InterlockedCompareExchange(ref _endpoints, after, before) != before);
            foreach (var connection in before)
            {
                connection.Dispose();
            }
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
        _logger.ZLogTrace($"{nameof(DisposeAsync)} {this}");
        ImmutableArray<IProtocolEndpoint> after, before;
        do
        {
            before = _endpoints;
            after = [];    
        }
        // check if the value is changed by another thread while we are removing the endpoint
        while (ImmutableInterlocked.InterlockedCompareExchange(ref _endpoints, after, before) != before);
        foreach (var connection in before)
        {
            connection.Dispose();
        }
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