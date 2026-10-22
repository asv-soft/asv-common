using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;
using Timeout = System.Threading.Timeout;

namespace Asv.IO;

public abstract class ProtocolPort<TConfig> : ProtocolConnection, IProtocolPort
    where TConfig : ProtocolPortConfig
{
    private readonly Lock _sync = new();
    private bool _enableRequested;
    private bool _transitionRunning;
    private int _transitionVersion;
    private int _interruptions;
    private Task _transitionTask = Task.CompletedTask;
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

    protected ProtocolPort(
        string id,
        TConfig config,
        IProtocolContext context,
        bool reconnectWhenEndpointError,
        IStatisticHandler? statistic = null
    )
        : base(id, context, statistic)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(id));
        }

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
            _protocols = [.. Context.AvailableProtocols.Where(x => hash.Contains(x.Id))];
        }

        foreach (var protocol in _protocols)
        {
            if (context.ParserFactory.ContainsKey(protocol.Id) == false)
            {
                throw new ArgumentException($"Parser for protocol {protocol} not found");
            }
        }
        _logger = context.LoggerFactory.CreateLogger<ProtocolPort<TConfig>>();
        _logger.ZLogInformation($"Create port {this} with config {config}");
    }

    private void ClearAndDisposeAllEndpoints()
    {
        ImmutableArray<IProtocolEndpoint> before;
        lock (_sync)
        {
            before = _endpoints;
            _endpoints = [];
        }
        foreach (var endpoint in before)
        {
            try
            {
                endpoint.Dispose();
                PublishEndpointRemoved(endpoint);
            }
            catch (Exception e)
            {
                _logger.ZLogError($"Error on dispose endpoint {endpoint}: {e.Message}");
                Debug.Fail("Error on dispose endpoint");
            }
        }
    }

    protected void RemoveAndDisposeEndpoint(
        IProtocolEndpoint endpoint,
        ProtocolConnectionException? err
    )
    {
        lock (_sync)
        {
            if (IsDisposed || !_endpoints.Contains(endpoint))
            {
                return;
            }
            _endpoints = _endpoints.Remove(endpoint);
        }

        try
        {
            endpoint.Dispose();
        }
        catch (Exception e)
        {
            _logger.ZLogError($"Error on dispose endpoint {endpoint}: {e.Message}");
            Debug.Fail("Error on dispose endpoint");
        }
        PublishEndpointRemoved(endpoint);
        if (err != null && _reconnectWhenEndpointError)
        {
            InternalRisePortErrorAndReconnect(err);
        }
    }

    protected void InternalAddEndpoint(IProtocolEndpoint endpoint) =>
        InternalAddEndpoint(endpoint, default);

    protected void InternalAddEndpoint(
        IProtocolEndpoint endpoint,
        CancellationToken generationCancel
    )
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        lock (_sync)
        {
            if (!IsDisposed && !endpoint.IsDisposed && !generationCancel.IsCancellationRequested)
            {
                _endpoints = _endpoints.Add(endpoint);
                endpoint
                    .LastError.Where(x => x != null)
                    .Subscribe(endpoint, (x, p) => RemoveAndDisposeEndpoint(p, x));
                if (IsDisposed || endpoint.IsDisposed || !_endpoints.Contains(endpoint))
                {
                    return;
                }
                if (endpoint is ProtocolConnection connection)
                {
                    connection.SubscribeRx(InternalPublishRxMessageAsync);
                }
                else
                {
                    endpoint.OnRxMessage.Subscribe(
                        InternalPublishRxMessage,
                        InternalPublishRxError,
                        _ => { }
                    );
                }
                if (!IsDisposed && _endpoints.Contains(endpoint))
                {
                    _endpointAdded.OnNext(endpoint);
                }
                return;
            }
        }
        endpoint.Dispose();
    }

    private void PublishEndpointRemoved(IProtocolEndpoint endpoint)
    {
        lock (_sync)
        {
            if (!IsDisposed)
            {
                _endpointRemoved.OnNext(endpoint);
            }
        }
    }

    protected ImmutableArray<IProtocolParser> InternalCreateParsers()
    {
        var parsers = ImmutableArray.CreateBuilder<IProtocolParser>(_protocols.Length);
        try
        {
            foreach (var protocol in _protocols)
            {
                parsers.Add(Context.ParserFactory[protocol.Id](_context, StatisticHandler));
            }
            return parsers.MoveToImmutable();
        }
        catch
        {
            foreach (var parser in parsers)
            {
                try
                {
                    parser.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.ZLogError(ex, $"Error on dispose parser for port {this}: {ex.Message}");
                }
            }
            throw;
        }
    }

    public override string DisplayName => _config is null
        ? Id
        : !string.IsNullOrWhiteSpace(_config.Name) ? _config.Name : DefaultDisplayName;
    public override string Description => _config is null
        ? Id
        : ProtocolConnectionDisplay.PortDescription(_config, _protocols);
    protected virtual string DefaultDisplayName => Id;

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
        lock (_sync)
        {
            if (IsDisposed || _enableRequested)
            {
                return;
            }
            _enableRequested = true;
            _transitionVersion++;
            _config.IsEnabled = true;
            _isEnabled.Value = true;
            QueueTransition();
        }
    }

    public void Disable()
    {
        CancellationTokenSource? cancellation;
        ITimer? timer;
        lock (_sync)
        {
            if (IsDisposed || !_enableRequested)
            {
                return;
            }
            _enableRequested = false;
            _transitionVersion++;
            _interruptions++;
            cancellation = _startStopCancel;
            timer = _reconnectTimer;
            _reconnectTimer = null;
            _config.IsEnabled = false;
            _isEnabled.Value = false;
        }
        try
        {
            timer?.Dispose();
            Cancel(cancellation);
            TryDisableTransport();
        }
        finally
        {
            lock (_sync)
            {
                _interruptions--;
                QueueTransition();
            }
        }
    }

    // Call with _sync held. Only this worker installs transports; interruption may
    // close a blocking connect, but the next generation waits for it to return.
    private void QueueTransition()
    {
        if (IsDisposed || _transitionRunning || _interruptions != 0)
        {
            return;
        }
        _transitionRunning = true;
        _transitionTask = Task.Run(ProcessTransitions);
    }

    private void ProcessTransitions()
    {
        while (true)
        {
            int version;
            bool enabled;
            CancellationTokenSource? previous;
            CancellationTokenSource? generation;
            CancellationToken token;
            lock (_sync)
            {
                if (IsDisposed || _interruptions != 0)
                {
                    _transitionRunning = false;
                    return;
                }
                version = _transitionVersion;
                enabled = _enableRequested;
                previous = _startStopCancel;
                generation = enabled ? new CancellationTokenSource() : null;
                _startStopCancel = generation;
                token = generation?.Token ?? default;
            }

            Cancel(previous);
            previous?.Dispose();
            try
            {
                lock (_sync)
                {
                    if (IsCurrentTransition(version))
                    {
                        _status.Value = ProtocolPortStatus.InProgress;
                    }
                }
                InternalSafeDisable();
                ClearAndDisposeAllEndpoints();
                if (enabled)
                {
                    token.ThrowIfCancellationRequested();
                    InternalSafeEnable(token);
                    token.ThrowIfCancellationRequested();
                }
                lock (_sync)
                {
                    if (IsCurrentTransition(version))
                    {
                        _status.Value = enabled
                            ? ProtocolPortStatus.Connected
                            : ProtocolPortStatus.Disconnected;
                    }
                }
            }
            catch (Exception ex)
            {
                var wasCancelled = token.IsCancellationRequested;
                Cancel(generation);
                TryDisableTransport();
                ClearAndDisposeAllEndpoints();
                lock (_sync)
                {
                    if (IsCurrentTransition(version) && !wasCancelled)
                    {
                        ScheduleReconnect(ex);
                    }
                }
            }

            lock (_sync)
            {
                if (IsDisposed || version == _transitionVersion || _interruptions != 0)
                {
                    _transitionRunning = false;
                    return;
                }
            }
        }
    }

    private bool IsCurrentTransition(int version) =>
        !IsDisposed && version == _transitionVersion && _interruptions == 0;

    protected void InternalRisePortErrorAndReconnect(Exception ex)
    {
        ClearAndDisposeAllEndpoints();
        lock (_sync)
        {
            ScheduleReconnect(ex);
        }
    }

    // Observer callbacks may synchronously disable or dispose the port.
    private void ScheduleReconnect(Exception ex)
    {
        if (IsDisposed || !_enableRequested)
        {
            return;
        }
        var version = _transitionVersion;
        _reconnectTimer?.Dispose();
        _reconnectTimer = null;
        _logger.ZLogError(ex, $"Port {this} error: {ex.Message}");
        _error.OnNext(new ProtocolPortException(this, $"Port {this} error:{ex.Message}", ex));
        if (!IsCurrentTransition(version) || !_enableRequested)
        {
            return;
        }
        _status.Value = ProtocolPortStatus.Error;
        if (!IsCurrentTransition(version) || !_enableRequested)
        {
            return;
        }
        try
        {
            var timer = _context.TimeProvider.CreateTimer(
                ReconnectAfterError,
                null,
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan
            );
            _reconnectTimer = timer;
            timer.Change(
                TimeSpan.FromMilliseconds(_config.ReconnectTimeoutMs),
                Timeout.InfiniteTimeSpan
            );
        }
        catch (Exception timerError)
        {
            _reconnectTimer?.Dispose();
            _reconnectTimer = null;
            _logger.ZLogError(timerError, $"Unable to schedule reconnect for port {this}");
        }
    }

    private void ReconnectAfterError(object? state)
    {
        lock (_sync)
        {
            _reconnectTimer?.Dispose();
            _reconnectTimer = null;
            if (
                IsDisposed
                || !_enableRequested
                || _status.CurrentValue == ProtocolPortStatus.Connected
            )
            {
                return;
            }
            _transitionVersion++;
            QueueTransition();
        }
    }

    private void Cancel(CancellationTokenSource? cancellation)
    {
        try
        {
            cancellation?.Cancel(false);
        }
        catch (ObjectDisposedException)
        {
            // A completed transition may already have released this source.
        }
        catch (AggregateException ex)
        {
            _logger.ZLogError(ex, $"Cancellation callback failed for port {this}: {ex.Message}");
        }
    }

    private void TryDisableTransport()
    {
        try
        {
            InternalSafeDisable();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Error on disable port {this}: {ex.Message}");
        }
    }

    protected abstract void InternalSafeDisable();
    protected abstract void InternalSafeEnable(CancellationToken token);

    public override async ValueTask Send(
        IProtocolMessage message,
        CancellationToken cancel = default
    )
    {
        ArgumentNullException.ThrowIfNull(message);
        if (IsDisposed)
        {
            return;
        }

        cancel.ThrowIfCancellationRequested();
        var newMessage = await InternalFilterTxMessage(message).ConfigureAwait(false);
        if (newMessage == null)
        {
            return;
        }

        var endpoints = _endpoints;
        foreach (var endpoint in endpoints)
        {
            await endpoint.Send(newMessage, cancel).ConfigureAwait(false);
        }
        InternalPublishTxMessage(newMessage);
    }

    public override string ToString()
    {
        return $"[PORT]({Id})";
    }

    #region Dispose

    private Task StopPort()
    {
        CancelPendingOperations();
        CancellationTokenSource? cancellation;
        ITimer? timer;
        Task transition;
        lock (_sync)
        {
            _transitionVersion++;
            cancellation = _startStopCancel;
            _startStopCancel = null;
            timer = _reconnectTimer;
            _reconnectTimer = null;
            transition = _transitionTask;
        }
        timer?.Dispose();
        try
        {
            Cancel(cancellation);
        }
        finally
        {
            cancellation?.Dispose();
            TryDisableTransport();
        }
        return transition;
    }

    private void DisposeObservables()
    {
        lock (_sync)
        {
            _endpointAdded.Dispose();
            _endpointRemoved.Dispose();
            _error.Dispose();
            _status.Dispose();
            _isEnabled.Dispose();
        }
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing)
            {
                StopPort();
                ClearAndDisposeAllEndpoints();
                DisposeObservables();
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        try
        {
            await StopPort().ConfigureAwait(false);
            ImmutableArray<IProtocolEndpoint> endpoints;
            lock (_sync)
            {
                endpoints = _endpoints;
                _endpoints = [];
            }
            foreach (var endpoint in endpoints)
            {
                await endpoint.DisposeAsync().ConfigureAwait(false);
            }
            DisposeObservables();
        }
        finally
        {
            await base.DisposeAsyncCore().ConfigureAwait(false);
        }
    }

    #endregion
}
