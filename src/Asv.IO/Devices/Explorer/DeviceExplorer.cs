using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using R3;
using ZLogger;

namespace Asv.IO;

public class ClientDeviceBrowserConfig
{
    public int DeviceTimeoutMs { get; set; } = 30_000;
    public int DeviceCheckIntervalMs { get; set; } = 1000;
}

public class DeviceExplorer : AsyncDisposableOnce, IDeviceExplorer
{
    #region Static

    public static IDeviceExplorer Create(IProtocolConnection connection, Action<IDeviceExplorerBuilder> configure)
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
    private readonly ObservableDictionary<DeviceId, IClientDevice> _devices = new();
    private readonly ConcurrentDictionary<DeviceId,long> _lastSeen = new();
    private readonly ILogger<DeviceExplorer> _logger;
    private readonly ITimer _timer;
    private readonly TimeSpan _deviceTimeout;
    private readonly ObservableList<IClientDevice> _deviceList;
    private readonly IDisposable _sub2;
    private readonly IDisposable _sub3;

    internal DeviceExplorer(ClientDeviceBrowserConfig config, IEnumerable<IClientDeviceFactory> providers, ImmutableArray<IClientDeviceExtender> extenders, IMicroserviceContext context)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(context);
        _extenders = extenders;
        _context = context;
        _logger = _context.LoggerFactory.CreateLogger<DeviceExplorer>();
        _providers = [..providers.OrderBy(x=>x.Order)];
        _sub1 = context.Connection.OnRxMessage.Subscribe(CheckNewDevice);
        _deviceTimeout = TimeSpan.FromMilliseconds(config.DeviceTimeoutMs);
        _timer = context.TimeProvider.CreateTimer(RemoveOldDevices, null, TimeSpan.FromMilliseconds(config.DeviceCheckIntervalMs), TimeSpan.FromMilliseconds(config.DeviceCheckIntervalMs));
        _deviceList = [];
        _sub2 = _devices.ObserveAdd().Subscribe(OnAddNewDevice);
        _sub3 = _devices.ObserveRemove().Subscribe(OnDeviceRemove);
    }

    private void OnDeviceRemove(CollectionRemoveEvent<KeyValuePair<DeviceId,IClientDevice>> e)
    {
        _deviceList.Remove(e.Value.Value);
    }

    private void OnAddNewDevice(CollectionAddEvent<KeyValuePair<DeviceId, IClientDevice>> e)
    {
        // we don't need to unsubscribe from this subscription because it will be disposed with Device itself
        e.Value.Value.State.Where(x=>x == ClientDeviceState.Complete).Take(1).Subscribe(e.Value.Value,(x,dev)=> _deviceList.Add(dev)); 
    }

    public IReadOnlyObservableDictionary<DeviceId, IClientDevice> Devices => _devices;

    public IReadOnlyObservableList<IClientDevice> InitializedDevices => _deviceList;

    private void RemoveOldDevices(object? state)
    {
        var itemsToDelete = _lastSeen
            .Where(x => _context.TimeProvider.GetElapsedTime(x.Value) >= _deviceTimeout).ToImmutableArray();
        if (itemsToDelete.Length == 0) return;
        _lock.EnterWriteLock();
        try
        {
            foreach (var item in itemsToDelete)
            {
                if (_devices.TryGetValue(item.Key, out var device))
                {
                    device.Dispose();
                }
                _lastSeen.TryRemove(item.Key, out _);
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

    private void CheckNewDevice(IProtocolMessage msg)
    {
        var providers = _providers;
        DeviceId? deviceId = null;
        IClientDeviceFactory? currentProvider = null;
        foreach (var provider in providers)
        {
            if (provider.TryIdentify(msg, out deviceId))
            {
                currentProvider = provider;
                break;
            }
        }
        if (deviceId == null || currentProvider == null) return;
        _lastSeen.AddOrUpdate(deviceId, _context.TimeProvider.GetTimestamp(), (_, _) => _context.TimeProvider.GetTimestamp());
        _lock.EnterUpgradeableReadLock();
        try
        {
            if (_devices.TryGetValue(deviceId, out var device1))
            {
                currentProvider.UpdateDevice(device1, msg);
                return;
            }
            _lock.EnterWriteLock();
            try
            {
                if (_devices.TryGetValue(deviceId, out var device2))
                {
                    currentProvider.UpdateDevice(device2, msg);
                    return;
                }
                var device = currentProvider.CreateDevice(msg, deviceId, _context, _extenders );
                _logger.ZLogInformation($"New device {deviceId} created by {currentProvider}");
                device.Initialize();
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

    protected override void Dispose(bool disposing)
    {
        _logger.ZLogTrace($"Dispose {nameof(DeviceExplorer)}");
        if (disposing)
        {
            _sub1.Dispose();
            _sub2.Dispose();
            _sub3.Dispose();
            _lock.Dispose();
            _timer.Dispose();
            _lastSeen.Clear();
            
            foreach (var device in _devices)
            {
                device.Value.Dispose();
            }
            _devices.Clear();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await CastAndDispose(_sub1);
        await CastAndDispose(_sub2);
        await CastAndDispose(_sub3);
        await CastAndDispose(_lock);
        _lastSeen.Clear();
        foreach (var device in _devices)
        {
            await device.Value.DisposeAsync();
        }
        _devices.Clear();
        await _timer.DisposeAsync();

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