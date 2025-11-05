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

public abstract class ClientDevice<TDeviceId> : AsyncDisposableWithCancel, IClientDevice
    where TDeviceId : DeviceId
{
    private readonly ClientDeviceConfig _config;
    private readonly ImmutableArray<IClientDeviceExtender> _extenders;
    private readonly ReactiveProperty<string?> _name;
    private readonly ReactiveProperty<ClientDeviceState> _state = new(
        ClientDeviceState.Uninitialized
    );
    private ImmutableArray<IMicroserviceClient> _microservices = [];
    private int _isTryReconnectInProgress;
    private readonly ILogger _logger;
    private ITimer? _reconnectionTimer;
    private int _isInitialized;

    protected ClientDevice(
        TDeviceId id,
        ClientDeviceConfig config,
        ImmutableArray<IClientDeviceExtender> extenders,
        IMicroserviceContext context
    )
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(context);
        _config = config;
        _extenders = extenders;
        Context = context;
        Id = id;
        _name = new ReactiveProperty<string?>(id.AsString());
        _logger = context.LoggerFactory.CreateLogger(id.AsString());
    }

    public void Initialize()
    {
        if (Interlocked.CompareExchange(ref _isInitialized, 1, 0) == 1)
        {
            _logger.ZLogTrace($"Skip double initialization [{Id}]");
            return;
        }

        try
        {
            InternalInitializeOnce();
            TryReconnect(null).SafeFireAndForget(); // call first time to create and init microservices
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, $"Error on initialize device [{Id}]: {e.Message}");
            throw;
        }
        finally
        {
            Interlocked.Exchange(ref _isInitialized, 0);
        }
    }

    /// <summary>
    /// This method is called only once, right after ctor
    /// </summary>
    protected virtual void InternalInitializeOnce()
    {
        // do nothing
    }

    // ReSharper disable once AsyncVoidMethod
    private async Task TryReconnect(object? state)
    {
        if (IsDisposed)
        {
            return; // do not reconnect if disposed
        }

        if (Interlocked.CompareExchange(ref _isTryReconnectInProgress, 1, 0) == 1)
        {
            _logger.ZLogTrace($"Skip double reconnect [{Id}]");
            return;
        }
        _reconnectionTimer?.Dispose();
        var builder = ImmutableArray.CreateBuilder<IMicroserviceClient>();
        try
        {
            _state.Value = ClientDeviceState.InProgress;
            await InitBeforeMicroservices(DisposeCancel).ConfigureAwait(false);

            await foreach (var item in InternalCreateMicroservices(DisposeCancel))
            {
                builder.Add(item);
                await item.Init(DisposeCancel);
            }

            foreach (var extender in _extenders)
            {
                await extender.Extend(Id, builder, DisposeCancel);
            }
            _microservices = builder.ToImmutable();
            await InitAfterMicroservices(DisposeCancel).ConfigureAwait(false);
            _state.Value = ClientDeviceState.Complete;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Error on connect/reconnect device [{Id}]: {ex.Message}");
            SafeDisposeMicroservices(builder.ToImmutable());
            if (IsDisposed)
            {
                return;
            }
            _state.Value = ClientDeviceState.Failed;
            _reconnectionTimer = Context.TimeProvider.CreateTimer(
                s => TryReconnect(s).SafeFireAndForget(),
                null,
                TimeSpan.FromMilliseconds(_config.RequestDelayAfterFailMs),
                Timeout.InfiniteTimeSpan
            );
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
        _name.Value = name;
    }

    public ReadOnlyReactiveProperty<string?> Name => _name;

    public ReadOnlyReactiveProperty<ClientDeviceState> State => _state;
    public abstract ILinkIndicator Link { get; }

    /// <summary>
    /// This method is called before the microservices are created
    /// Can be called multiple times, if initialization fails.
    /// </summary>
    protected virtual ValueTask InitBeforeMicroservices(CancellationToken cancel)
    {
        return ValueTask.CompletedTask;
    }

    protected abstract IAsyncEnumerable<IMicroserviceClient> InternalCreateMicroservices(
        CancellationToken cancel
    );

    /// <summary>
    /// This method is called after the microservices have been created and initialized.
    /// Can be called multiple times, if initialization fails.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    protected virtual ValueTask InitAfterMicroservices(CancellationToken cancel)
    {
        return ValueTask.CompletedTask;
    }

    public ImmutableArray<IMicroserviceClient> Microservices => _microservices;

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        _logger.ZLogTrace($"Dispose {Id}");
        if (disposing)
        {
            _reconnectionTimer?.Dispose();
            _name.Dispose();
            _state.Dispose();
            SafeDisposeMicroservices(_microservices);
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_reconnectionTimer != null)
        {
            await _reconnectionTimer.DisposeAsync();
        }

        await CastAndDispose(_name);
        await CastAndDispose(_state);
        SafeDisposeMicroservices(_microservices);
        _microservices = ImmutableArray<IMicroserviceClient>.Empty;
        await base.DisposeAsyncCore();

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync();
            }
            else
            {
                resource.Dispose();
            }
        }
    }

    #endregion
}
