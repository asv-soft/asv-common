using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;

namespace Asv.IO;

public class ClientDeviceConfig
{
    public int RequestDelayAfterFailMs { get; set; } = 1000;
}

public static class ClientDevice
{
    #region Static

    

    #endregion
}

public abstract class ClientDevice<TDeviceId> : AsyncDisposableWithCancel, IClientDevice
    where TDeviceId:DeviceId
{
    
    private readonly ClientDeviceConfig _config;
    private readonly ImmutableArray<IClientDeviceExtender> _extenders;
    private readonly ReactiveProperty<string?> _name;
    private readonly ReactiveProperty<ClientDeviceState> _state = new(ClientDeviceState.Uninitialized);
    private ImmutableArray<IMicroserviceClient> _microservices;
    private int _isTryReconnectInProgress;
    private readonly ILogger _logger;
    private bool _needToRequestAgain;
    private ITimer? _reconnectionTimer;
    private IDisposable? _sub1;
    private IDisposable? _sub2;
    


    protected ClientDevice(TDeviceId id, ClientDeviceConfig config, ImmutableArray<IClientDeviceExtender> extenders, IMicroserviceContext context)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(context);
        _config = config;
        _extenders = extenders;
        Context = context;
        Id = id;
        _name = new ReactiveProperty<string?>(id.ToString());
        _logger = context.LoggerFactory.CreateLogger(id.AsString());
        Task.Factory.StartNew(InternalInitFirst);
    }

    private void InternalInitFirst()
    {
        _sub1 = Link.State.DistinctUntilChanged()
            .Where(s => s == LinkState.Disconnected)
            .Subscribe(_ => _needToRequestAgain = true);

        _sub2 = Link.State.DistinctUntilChanged()
            .Where(_ => _needToRequestAgain)
            .Where(s => s == LinkState.Connected)
            .Subscribe(_ => TryReconnect(null));
    }

    // ReSharper disable once AsyncVoidMethod
    private async void TryReconnect(object? state)
    {
        if (Interlocked.CompareExchange(ref _isTryReconnectInProgress, 0, 1) == 1)
        {
            _logger.ZLogTrace($"Skip double reconnect [{Id}]");
            return;
        }
        _reconnectionTimer?.Dispose();
        using var reconnectCancel = new CancellationTokenSource();
        using var combine = CancellationTokenSource.CreateLinkedTokenSource(DisposeCancel, reconnectCancel.Token);
        var builder = ImmutableArray.CreateBuilder<IMicroserviceClient>();
        try
        {
            _state.OnNext(ClientDeviceState.InProgress);
            await InitBeforeMicroservices(combine.Token).ConfigureAwait(false);
            
            await foreach (var item in InternalCreateMicroservices(combine.Token))
            {
                builder.Add(item);
                await item.Init(combine.Token);
            }

            foreach (var extender in _extenders)
            {
                await extender.Extend(Id, builder, combine.Token);
            }
            _microservices = builder.ToImmutable();
            await InitAfterMicroservices(combine.Token).ConfigureAwait(false);
            _state.OnNext(ClientDeviceState.Complete);
            _needToRequestAgain = false;
        }
        catch (Exception ex)
        {
            combine.Cancel(false);
            _logger.ZLogError(ex, $"Error on connect/reconnect device [{Id}]: {ex.Message}");
            SafeDisposeMicroservices(builder.ToImmutable());
            _state.OnNext(ClientDeviceState.Failed);
            _needToRequestAgain = true;
            _reconnectionTimer = Context.TimeProvider.CreateTimer(TryReconnect, null,
                TimeSpan.FromMilliseconds(_config.RequestDelayAfterFailMs),Timeout.InfiniteTimeSpan);
        }
        finally
        {
            Interlocked.Exchange(ref _isTryReconnectInProgress, 0);
        }
    }

    private void SafeDisposeMicroservices(ImmutableArray<IMicroserviceClient> items)
    {
        foreach (var item in items)
        {
            try
            {
                item.Dispose();
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"Error on dispose microservice {item}");
            }
        }
    }

    public DeviceId Id { get; }
    
    protected IMicroserviceContext Context { get; }

    protected void UpdateDeviceName(string? name)
    {
        _name.OnNext(name);
    }

    public ReadOnlyReactiveProperty<string?> Name => _name;

    public ReadOnlyReactiveProperty<ClientDeviceState> State => _state;
    public abstract ILinkIndicator Link { get; }

    
    /// <summary>
    /// This method is called before the microservices are created
    /// Can be called multiple times, if initialization fails.
    /// </summary>
    protected virtual Task InitBeforeMicroservices(CancellationToken cancel)
    {
        return Task.CompletedTask;
    }
    protected abstract IAsyncEnumerable<IMicroserviceClient> InternalCreateMicroservices(CancellationToken cancel);
    /// <summary>
    /// This method is called after the microservices have been created and initialized.
    /// Can be called multiple times, if initialization fails.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    protected virtual Task InitAfterMicroservices(CancellationToken cancel)
    {
        return Task.CompletedTask;
    }
    
    public ImmutableArray<IMicroserviceClient> Microservices => _microservices;

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _name.Dispose();
            _state.Dispose();
            _reconnectionTimer?.Dispose();
            _sub1?.Dispose();
            _sub2?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await CastAndDispose(_name);
        await CastAndDispose(_state);
        if (_reconnectionTimer != null) await _reconnectionTimer.DisposeAsync();
        if (_sub1 != null) await CastAndDispose(_sub1);
        if (_sub2 != null) await CastAndDispose(_sub2);
        SafeDisposeMicroservices(_microservices);
        _microservices = ImmutableArray<IMicroserviceClient>.Empty;
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