using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Asv.IO;

public class ClientDeviceBrowserBuilder : IClientDeviceBrowserBuilder,IClientDeviceExtenderBuilder,IClientDeviceFactoryBuilder
{
    private readonly IProtocolConnection _connection;
    private ILoggerFactory _loggerFactory;
    private TimeProvider _timeProvider;
    private IMeterFactory _meterFactory;
    private ClientDeviceBrowserConfig _config = new();
    private readonly List<IClientDeviceFactory> _factories = new();
    private readonly ImmutableArray<IClientDeviceExtender>.Builder _extenders = ImmutableArray.CreateBuilder<IClientDeviceExtender>();

    public ClientDeviceBrowserBuilder(IProtocolConnection connection)
    {
        _connection = connection;
        _loggerFactory = NullLoggerFactory.Instance;
        _timeProvider = TimeProvider.System;
        _meterFactory = new DefaultMeterFactory();
    }
    public void SetConfig(ClientDeviceBrowserConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;
    }
    public void SetDefaultConfig()
    {
        _config = new ClientDeviceBrowserConfig();
    }

    public IClientDeviceExtenderBuilder Extenders => this;
    
    public IClientDeviceFactoryBuilder Factories => this;

    public void SetLog(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _loggerFactory = loggerFactory;
    }

    public void SetDefaultLog()
    {
        _loggerFactory = NullLoggerFactory.Instance;
    }

    public void SetTimeProvider(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    public void SetDefaultTimeProvider()
    {
        _timeProvider = TimeProvider.System;
    }

    public void SetMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        _meterFactory = meterFactory;
    }

    public void SetDefaultMetrics()
    {
        _meterFactory = new DefaultMeterFactory();
    }
    
    public IClientDeviceBrowser Build()
    {
        return new ClientDeviceBrowser(_config, _factories, _extenders.ToImmutable(), new DeviceContext(_connection, _loggerFactory, _timeProvider, _meterFactory));
    }

    public void Register(IClientDeviceExtender extender)
    {
        ArgumentNullException.ThrowIfNull(extender);
        _extenders.Add(extender);
    }

    public void Register(IClientDeviceFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factories.Add(factory);
    }

    void IClientDeviceFactoryBuilder.Clear()
    {
        _factories.Clear();
    }

    void IClientDeviceExtenderBuilder.Clear()
    {
        _extenders.Clear();
    }
}