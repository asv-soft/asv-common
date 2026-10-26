using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using R3;
using ZLogger;

namespace Asv.IO;

/// <summary>
/// Configures removal of discovered devices that no longer produce accepted discovery traffic.
/// </summary>
/// <remarks>
/// <see cref="DeviceExplorer"/> converts these millisecond values to time spans at construction.
/// Changing this object afterward does not reconfigure an existing explorer.
/// </remarks>
public class ClientDeviceBrowserConfig
{
    /// <summary>
    /// Gets or sets the inactivity interval after which a discovered device is eligible for removal.
    /// </summary>
    /// <value>The timeout in milliseconds; the default is <c>30000</c> (30 seconds).</value>
    /// <remarks>
    /// The timer removes devices when elapsed time since their last accepted discovery message reaches
    /// this interval. Address extraction, live attribution queries, and traffic rejected by discovery
    /// classifiers do not renew it. Removal occurs on a periodic check rather than precisely at the deadline.
    /// </remarks>
    public int DeviceTimeoutMs { get; set; } = 30_000;

    /// <summary>
    /// Gets or sets the initial delay and period of the inactive-device removal timer.
    /// </summary>
    /// <value>The check interval in milliseconds; the default is <c>1000</c> (1 second).</value>
    /// <remarks>
    /// The configured <see cref="TimeProvider"/> creates the timer and determines accepted timer intervals.
    /// This property does not validate the assigned value itself.
    /// </remarks>
    public int DeviceCheckIntervalMs { get; set; } = 1000;
}

/// <summary>
/// Discovers and owns client devices from received protocol traffic and resolves their canonical source addresses.
/// </summary>
/// <remarks>
/// <para>
/// Factories are captured in an immutable snapshot ordered by ascending
/// <see cref="IClientDeviceFactory.Order"/>. Incoming traffic is offered to discovery classifiers;
/// accepted traffic creates or updates devices and refreshes their inactivity timestamps. Ordinary
/// traffic with an extractable source address alone does not refresh that timeout.
/// </para>
/// <para>
/// Discovery binds compatible source and device addresses to the discovered identity. Conflicting
/// valid addresses are left unbound. A previously bound address reuses its owning device across
/// transport channels and is updated through that device's creating factory.
/// </para>
/// <para>
/// Disposal stops subscriptions and the removal timer, clears the catalog and address bindings,
/// and disposes owned devices. The supplied connection, factories, and extender objects are not
/// disposed by the explorer. Pure address extraction remains available from the factory snapshot.
/// </para>
/// </remarks>
/// <seealso cref="IDeviceExplorer"/>
public class DeviceExplorer : AsyncDisposableOnce, IDeviceExplorer
{
    #region Static

    /// <summary>
    /// Builds an explorer and immediately subscribes it to the supplied connection's received messages.
    /// </summary>
    /// <param name="connection">The caller-owned protocol connection to observe and share with created devices.</param>
    /// <param name="configure">The callback that configures services, timing, factories, and device extenders before construction.</param>
    /// <returns>A running explorer that owns its discovered devices, receive subscriptions, and removal timer.</returns>
    /// <remarks>
    /// Register factories through <see cref="IDeviceExplorerBuilder.Factories"/> in
    /// <paramref name="configure"/>; the builder starts without factory registrations. Dispose the
    /// returned explorer when finished. Its disposal does not dispose <paramref name="connection"/>.
    /// Defaults are a 30-second device timeout, a 1-second removal check, system time, and null logging.
    /// Neither argument is explicitly checked for null by this method or the connection's builder.
    /// </remarks>
    /// <exception cref="NullReferenceException">
    /// <paramref name="configure"/> is null, or <paramref name="connection"/> is null when the receive
    /// subscription is created after the callback returns.
    /// </exception>
    public static IDeviceExplorer Create(
        IProtocolConnection connection,
        Action<IDeviceExplorerBuilder> configure
    )
    {
        var builder = new DeviceExplorerBuilder(connection);
        configure(builder);
        return builder.Build();
    }

    #endregion

    private readonly ImmutableArray<IClientDeviceExtender> _extenders;
    private readonly IMicroserviceContext _context;
    private readonly ImmutableArray<IClientDeviceFactory> _providers;
    private readonly IDisposable _sub1;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ObservableDictionary<DeviceId, IClientDevice> _devices = new(
        DeviceIdComparer.Instance
    );
    private readonly ConcurrentDictionary<DeviceId, long> _lastSeen = new(
        DeviceIdComparer.Instance
    );
    private readonly Dictionary<DeviceId, IClientDeviceFactory> _deviceFactories = new(
        DeviceIdComparer.Instance
    );
    private readonly Dictionary<DeviceId, DeviceAddress> _deviceAddresses = new(
        DeviceIdComparer.Instance
    );
    private readonly Dictionary<DeviceAddress, DeviceId> _addresses = new();
    private readonly ILogger<DeviceExplorer> _logger;
    private readonly ITimer _timer;
    private readonly TimeSpan _deviceTimeout;
    private readonly ObservableList<IClientDevice> _deviceList;
    private readonly IDisposable _sub2;
    private readonly IDisposable _sub3;

    internal DeviceExplorer(
        ClientDeviceBrowserConfig config,
        IEnumerable<IClientDeviceFactory> providers,
        ImmutableArray<IClientDeviceExtender> extenders,
        IMicroserviceContext context
    )
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(context);
        _extenders = extenders;
        _context = context;
        _logger = _context.LoggerFactory.CreateLogger<DeviceExplorer>();
        _providers = [.. providers.OrderBy(x => x.Order)];
        _sub1 = context.Connection.OnRxMessage.Subscribe(CheckNewDevice);
        _deviceTimeout = TimeSpan.FromMilliseconds(config.DeviceTimeoutMs);
        _timer = context.TimeProvider.CreateTimer(
            RemoveOldDevices,
            null,
            TimeSpan.FromMilliseconds(config.DeviceCheckIntervalMs),
            TimeSpan.FromMilliseconds(config.DeviceCheckIntervalMs)
        );
        _deviceList = [];
        _sub2 = _devices.ObserveAdd().Subscribe(OnAddNewDevice);
        _sub3 = _devices.ObserveRemove().Subscribe(OnDeviceRemove);
    }

    private void OnDeviceRemove(CollectionRemoveEvent<KeyValuePair<DeviceId, IClientDevice>> e)
    {
        _deviceList.Remove(e.Value.Value);
    }

    private void OnAddNewDevice(CollectionAddEvent<KeyValuePair<DeviceId, IClientDevice>> e)
    {
        // we don't need to unsubscribe from this subscription because it will be disposed with Device itself
        e.Value.Value.State.Where(x => x == ClientDeviceState.Complete)
            .Take(1)
            .Subscribe(e.Value.Value, (_, device) => OnDeviceInitialized(device));
    }

    private void OnDeviceInitialized(IClientDevice device)
    {
        if (IsDisposed)
        {
            return;
        }

        var ownsLock = _lock.IsWriteLockHeld;
        if (!ownsLock)
        {
            try
            {
                _lock.EnterWriteLock();
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }
        try
        {
            if (
                !IsDisposed
                && _devices.TryGetValue(device.Id, out var registered)
                && ReferenceEquals(registered, device)
            )
            {
                _deviceList.Add(device);
            }
        }
        finally
        {
            if (!ownsLock)
            {
                _lock.ExitWriteLock();
            }
        }
    }

    /// <inheritdoc cref="IDeviceExplorer.Devices"/>
    public IReadOnlyObservableDictionary<DeviceId, IClientDevice> Devices => _devices;

    /// <inheritdoc cref="IDeviceExplorer.InitializedDevices"/>
    public IReadOnlyObservableList<IClientDevice> InitializedDevices => _deviceList;

    /// <inheritdoc cref="IDeviceExplorer.TryIdentify"/>
    /// <remarks>
    /// Resolves the first valid source address extracted by the ordered factories against current
    /// discovery bindings. Returns the bound discovered identity, without requiring the message's
    /// factory classifier to accept it. Disposal and timeout removal invalidate those bindings.
    /// </remarks>
    public bool TryIdentify(IProtocolMessage message, [NotNullWhen(true)] out DeviceId? deviceId)
    {
        ArgumentNullException.ThrowIfNull(message);
        deviceId = null;
        if (IsDisposed)
        {
            return false;
        }

        // Collection observers can query attribution while the write lock is held.
        var ownsLock =
            _lock.IsWriteLockHeld || _lock.IsReadLockHeld || _lock.IsUpgradeableReadLockHeld;
        if (!ownsLock)
        {
            try
            {
                _lock.EnterReadLock();
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }
        try
        {
            return !IsDisposed
                && TryGetSourceAddress(message, out var address)
                && _addresses.TryGetValue(address, out deviceId);
        }
        finally
        {
            if (!ownsLock)
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <inheritdoc cref="IDeviceExplorer.TryGetSourceAddress"/>
    /// <remarks>
    /// Consults the immutable factory snapshot in ascending order, returning the first successful
    /// address with nonempty, non-whitespace family and value strings. Invalid successful results are
    /// skipped. This operation does not access the device catalog and remains available after disposal.
    /// </remarks>
    public bool TryGetSourceAddress(IProtocolMessage message, out DeviceAddress address)
    {
        ArgumentNullException.ThrowIfNull(message);
        foreach (var provider in _providers)
        {
            if (provider.TryGetSourceAddress(message, out address) && IsValid(address))
            {
                return true;
            }
        }
        address = default;
        return false;
    }

    /// <inheritdoc cref="IDeviceExplorer.TryGetDeviceAddress"/>
    /// <remarks>
    /// Consults the immutable factory snapshot in ascending order, returning the first successful
    /// address with nonempty, non-whitespace family and value strings. Invalid successful results are
    /// skipped. The identity need not be registered, and conversion remains available after disposal.
    /// </remarks>
    public bool TryGetDeviceAddress(DeviceId deviceId, out DeviceAddress address)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        foreach (var provider in _providers)
        {
            if (provider.TryGetDeviceAddress(deviceId, out address) && IsValid(address))
            {
                return true;
            }
        }
        address = default;
        return false;
    }

    private static bool IsValid(DeviceAddress address) =>
        !string.IsNullOrWhiteSpace(address.Family) && !string.IsNullOrWhiteSpace(address.Value);

    private bool TryGetDiscoveryAddress(
        IProtocolMessage message,
        DeviceId deviceId,
        IClientDeviceFactory factory,
        out DeviceAddress address
    )
    {
        var hasSource = TryGetSourceAddress(message, out address);
        var hasDevice =
            factory.TryGetDeviceAddress(deviceId, out var deviceAddress) && IsValid(deviceAddress);
        if (hasSource && hasDevice && address != deviceAddress)
        {
            _logger.ZLogWarning(
                $"Conflicting source and device addresses for {deviceId}; skip address binding"
            );
            address = default;
            return false;
        }
        if (hasSource)
        {
            return true;
        }
        address = deviceAddress;
        return hasDevice;
    }

    private void RemoveOldDevices(object? state)
    {
        var itemsToDelete = _lastSeen
            .Where(x => _context.TimeProvider.GetElapsedTime(x.Value) >= _deviceTimeout)
            .ToImmutableArray();
        if (itemsToDelete.Length == 0)
        {
            return;
        }

        _lock.EnterWriteLock();
        try
        {
            foreach (var item in itemsToDelete)
            {
                if (
                    !_lastSeen.TryGetValue(item.Key, out var lastSeen)
                    || lastSeen != item.Value
                    || _context.TimeProvider.GetElapsedTime(lastSeen) < _deviceTimeout
                )
                {
                    continue;
                }
                if (_devices.TryGetValue(item.Key, out var device))
                {
                    try
                    {
                        device.Dispose();
                    }
                    catch (Exception e)
                    {
                        _logger.ZLogError(e, $"Error to dispose device: {e.Message}");
                    }
                }
                _lastSeen.TryRemove(item.Key, out _);
                _deviceFactories.Remove(item.Key);
                if (
                    _deviceAddresses.Remove(item.Key, out var address)
                    && _addresses.TryGetValue(address, out var boundId)
                    && ReferenceEquals(boundId, item.Key)
                )
                {
                    _addresses.Remove(address);
                }
                _devices.Remove(item.Key);
                _logger.ZLogInformation($"Remove device {item.Key}");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error on remove old devices");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private bool TryIdentify(
        IProtocolMessage message,
        [NotNullWhen(true)] out DeviceId? deviceId,
        [NotNullWhen(true)] out IClientDeviceFactory? factory
    )
    {
        ArgumentNullException.ThrowIfNull(message);
        foreach (var provider in _providers)
        {
            if (provider.TryIdentify(message, out deviceId) && deviceId != null)
            {
                factory = provider;
                return true;
            }
        }
        deviceId = null;
        factory = null;
        return false;
    }

    private void CheckNewDevice(IProtocolMessage msg)
    {
        if (!TryIdentify(msg, out var deviceId, out var currentProvider))
        {
            return;
        }

        _lock.EnterUpgradeableReadLock();
        try
        {
            if (IsDisposed)
            {
                return;
            }
            _lock.EnterWriteLock();
            try
            {
                var hasAddress = TryGetDiscoveryAddress(
                    msg,
                    deviceId,
                    currentProvider,
                    out var address
                );
                if (hasAddress && _addresses.TryGetValue(address, out var existingId))
                {
                    _lastSeen[existingId] = _context.TimeProvider.GetTimestamp();
                    _deviceFactories[existingId].UpdateDevice(_devices[existingId], msg);
                    return;
                }
                if (_devices.TryGetValue(deviceId, out var existing))
                {
                    _lastSeen[existing.Id] = _context.TimeProvider.GetTimestamp();
                    currentProvider.UpdateDevice(existing, msg);
                    return;
                }
                var device = currentProvider.CreateDevice(msg, deviceId, _context, _extenders);
                _logger.ZLogInformation($"New device {deviceId} created by {currentProvider}");
                device.Initialize();
                _lastSeen[deviceId] = _context.TimeProvider.GetTimestamp();
                _deviceFactories.Add(deviceId, currentProvider);
                if (hasAddress)
                {
                    _addresses.Add(address, deviceId);
                    _deviceAddresses.Add(deviceId, address);
                }
                _devices.Add(deviceId, device);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    #region Dispose

    /// <inheritdoc cref="AsyncDisposableOnce.Dispose(bool)"/>
    /// <remarks>
    /// When disposing managed resources, stops receive and collection subscriptions and the timer,
    /// clears discovery state, and synchronously disposes the owned devices. The shared connection
    /// and registered factory and extender objects are not disposed here.
    /// </remarks>
    /// <param name="disposing"><see langword="true"/> to release managed resources.</param>
    protected override void Dispose(bool disposing)
    {
        _logger.ZLogTrace($"Dispose {nameof(DeviceExplorer)}");
        if (disposing)
        {
            _sub1.Dispose();
            _sub2.Dispose();
            _sub3.Dispose();
            _timer.Dispose();
            IClientDevice[] devices;
            _lock.EnterWriteLock();
            try
            {
                devices = ClearDevices();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            foreach (var device in devices)
            {
                device.Dispose();
            }

            _lock.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc cref="AsyncDisposableOnce.DisposeAsyncCore"/>
    /// <remarks>
    /// Stops subscriptions and the timer, clears discovery state, and awaits disposal of each owned
    /// device. Subscriptions supporting only synchronous disposal are disposed synchronously.
    /// The shared connection and registered factory and extender objects remain caller-owned.
    /// </remarks>
    /// <returns>A task-like value representing completion of managed-resource disposal.</returns>
    protected override async ValueTask DisposeAsyncCore()
    {
        await CastAndDispose(_sub1).ConfigureAwait(false);
        await CastAndDispose(_sub2).ConfigureAwait(false);
        await CastAndDispose(_sub3).ConfigureAwait(false);
        await _timer.DisposeAsync().ConfigureAwait(false);

        IClientDevice[] devices;
        _lock.EnterWriteLock();
        try
        {
            devices = ClearDevices();
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        foreach (var device in devices)
        {
            await device.DisposeAsync().ConfigureAwait(false);
        }

        _lock.Dispose();
        await base.DisposeAsyncCore().ConfigureAwait(false);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                resource.Dispose();
            }
        }
    }

    private IClientDevice[] ClearDevices()
    {
        var devices = _devices.Select(kvp => kvp.Value).ToArray();
        _lastSeen.Clear();
        _deviceFactories.Clear();
        _deviceAddresses.Clear();
        _addresses.Clear();
        _devices.Clear();
        _deviceList.Clear();
        return devices;
    }

    private sealed class DeviceIdComparer : IEqualityComparer<DeviceId>
    {
        public static DeviceIdComparer Instance { get; } = new();

        public bool Equals(DeviceId? x, DeviceId? y) => object.Equals(x, y);

        public int GetHashCode(DeviceId obj) => obj.GetHashCode();
    }

    #endregion
}
