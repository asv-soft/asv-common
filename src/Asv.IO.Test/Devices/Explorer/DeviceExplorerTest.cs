using System;
using System.Threading.Tasks;
using Asv.Cfg.Test;
using Asv.IO.Device;
using JetBrains.Annotations;
using TimeProviderExtensions;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test.Devices.Explorer;

[TestSubject(typeof(DeviceExplorer))]
public class DeviceExplorerTest:IDisposable
{
    private readonly ManualTimeProvider _routerTime;
    private readonly IVirtualConnection _link;
    private readonly IDeviceExplorer _explorer;
    private readonly ClientDeviceBrowserConfig _explorerConfig;

    public DeviceExplorerTest(ITestOutputHelper log)
    {
        _routerTime = new ManualTimeProvider();
        var loggerFactory = new TestLoggerFactory(log, _routerTime, "ROUTER");
        var protocol = Protocol.Create(builder =>
        {
            builder.SetLog(loggerFactory);
            builder.SetTimeProvider(_routerTime);
            builder.Protocols.RegisterExampleProtocol();
            builder.Formatters.RegisterSimpleFormatter();
        });
        _link = protocol.CreateVirtualConnection();
      
        _explorerConfig = new ClientDeviceBrowserConfig
        {
            DeviceTimeoutMs = 100,
            DeviceCheckIntervalMs = 100
        };
        _explorer = DeviceExplorer.Create(_link.Client, builder =>
        {
            builder.SetTimeProvider(_routerTime);
            builder.SetLog(loggerFactory);
            builder.SetConfig(_explorerConfig);
            builder.Factories.RegisterExampleDevice(new ExampleDeviceConfig());
        });
    }

    [Fact]
    public async Task DeviceList_CreateAndRemoveDevices_Success()
    {
        Assert.Equal(0, _explorer.Devices.Count);

        await _link.Server.Send(new ExampleMessage1 { SenderId = 1 });
        Assert.Equal(1, _explorer.Devices.Count);
        
        await _link.Server.Send(new ExampleMessage1 { SenderId = 1 });
        Assert.Equal(1, _explorer.Devices.Count);
        
        await _link.Server.Send(new ExampleMessage1 { SenderId = 3 });
        Assert.Equal(2, _explorer.Devices.Count);
        
        _routerTime.Advance(TimeSpan.FromMilliseconds(Math.Max(_explorerConfig.DeviceTimeoutMs, _explorerConfig.DeviceCheckIntervalMs)));
        Assert.Equal(0, _explorer.Devices.Count);
    }

    public void Dispose()
    {
        _explorer.Dispose();
        _link.Dispose();
    }
}