using System;
using System.Threading.Tasks;
using Asv.Cfg.Test;
using Asv.IO.Device;
using Asv.XUnit;
using JetBrains.Annotations;
using TimeProviderExtensions;
using Xunit;

namespace Asv.IO.Test.Devices.Explorer;

[TestSubject(typeof(DeviceExplorer))]
public class DeviceExplorerTest : IDisposable
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
            DeviceCheckIntervalMs = 100,
        };
        _explorer = DeviceExplorer.Create(
            _link.Client,
            builder =>
            {
                builder.SetTimeProvider(_routerTime);
                builder.SetLog(loggerFactory);
                builder.SetConfig(_explorerConfig);
                builder.Factories.RegisterExampleDevice(new ExampleDeviceConfig());
            }
        );
    }

    [Fact]
    public async Task DeviceList_CreateAndRemoveDevices_Success()
    {
        Assert.Empty(_explorer.Devices);

        await _link.Server.Send(
            new ExampleMessage1 { SenderId = 1 },
            TestContext.Current.CancellationToken
        );
        Assert.Single(_explorer.Devices);

        await _link.Server.Send(
            new ExampleMessage1 { SenderId = 1 },
            TestContext.Current.CancellationToken
        );
        Assert.Single(_explorer.Devices);

        await _link.Server.Send(
            new ExampleMessage1 { SenderId = 3 },
            TestContext.Current.CancellationToken
        );
        Assert.Equal(2, _explorer.Devices.Count);

        _routerTime.Advance(
            TimeSpan.FromMilliseconds(
                Math.Max(
                    _explorerConfig.DeviceTimeoutMs + 1,
                    _explorerConfig.DeviceCheckIntervalMs + 1
                )
            )
        );
        Assert.Empty(_explorer.Devices);
    }

    [Fact]
    public async Task DeviceList_FactoriesRegisteredOutOfOrder_UsesFirstMatchingPriority()
    {
        var fallback = new OrderedFactory(10, 3);
        var preferred = new OrderedFactory(0, 2);
        var rejected = new OrderedFactory(-10, null);
        using var explorer = DeviceExplorer.Create(_link.Client, builder =>
        {
            builder.Factories.Register(fallback);
            builder.Factories.Register(preferred);
            builder.Factories.Register(rejected);
        });

        await _link.Server.Send(new ExampleMessage1(), TestContext.Current.CancellationToken);

        var device = Assert.Single(explorer.Devices);
        Assert.Equal(2, Assert.IsType<ExampleDeviceId>(device.Key).TargetId);
        Assert.Equal(1, rejected.Calls);
        Assert.Equal(1, preferred.Calls);
        Assert.Equal(0, fallback.Calls);
    }

    private sealed class OrderedFactory(int order, byte? targetId)
        : ExampleDeviceFactory(new ExampleDeviceConfig())
    {
        public override int Order => order;
        public int Calls { get; private set; }

        protected override bool InternalTryIdentify(ExampleMessageBase msg, out ExampleDeviceId? deviceId)
        {
            Calls++;
            deviceId = targetId.HasValue ? new ExampleDeviceId("test", targetId.Value) : null;
            return deviceId != null;
        }
    }

    public void Dispose()
    {
        _explorer.Dispose();
        _link.Dispose();
    }
}
