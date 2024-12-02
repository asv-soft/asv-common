using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using DotNext.Collections.Generic;
using DotNext.Threading;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using R3;
using ZLogger;

namespace Asv.IO;


public interface IClientDeviceProvider
{
    int Order { get; }
    DeviceClass DeviceClass { get; }
    bool TryIdentify(IProtocolMessage message, out DeviceId? deviceId);
    void UpdateDevice(IClientDevice device, IProtocolMessage message);
    IClientDevice CreateDevice(IProtocolMessage message, DeviceId deviceId);
}


public interface IClientDeviceBrowser
{
    
}

public class ClientDeviceBrowserConfig
{
    public int DeviceTimeoutMs { get; set; } = 30_000;
    public int DeviceCheckIntervalMs { get; set; } = 1000;
}

public class ClientDeviceBrowser : AsyncDisposableOnce, IClientDeviceBrowser
{
    private readonly IServiceContext _context;
    private readonly ImmutableArray<IClientDeviceProvider> _providers;
    private readonly IDisposable _sub1;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ObservableDictionary<DeviceId, IClientDevice> _devices = new();
    private readonly ConcurrentDictionary<DeviceId,long> _lastSeen = new();
    private readonly ILogger<ClientDeviceBrowser> _logger;
    private readonly ITimer _timer;
    private readonly TimeSpan _deviceTimeout;

    public ClientDeviceBrowser(ClientDeviceBrowserConfig config, IEnumerable<IClientDeviceProvider> providers, IServiceContext context)
    {
        _context = context;
        _logger = _context.Log.CreateLogger<ClientDeviceBrowser>();
        _providers = [..providers.OrderBy(x=>x.Order)];
        _sub1 = context.Connection.OnRxMessage.Subscribe(CheckNewDevice);
        _deviceTimeout = TimeSpan.FromMilliseconds(config.DeviceTimeoutMs);
        _timer = context.TimeProvider.CreateTimer(RemoveOldDevices, null, TimeSpan.FromMilliseconds(config.DeviceCheckIntervalMs), TimeSpan.FromMilliseconds(config.DeviceCheckIntervalMs));
    }

    private void RemoveOldDevices(object? state)
    {
        var itemsToDelete = _lastSeen
            .Where(x => _context.TimeProvider.GetElapsedTime(x.Value) > _deviceTimeout).ToImmutableArray();
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
        IClientDeviceProvider? currentProvider = null;
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
                device1.Provider.UpdateDevice(device1, msg);
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
                var device = currentProvider.CreateDevice(msg, deviceId);
                _logger.ZLogInformation($"New device {deviceId} created by {currentProvider}");
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
        if (disposing)
        {
            _sub1.Dispose();
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