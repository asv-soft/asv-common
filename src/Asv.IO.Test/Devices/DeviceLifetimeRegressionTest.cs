using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Asv.Common;
using Asv.IO.Device;
using Xunit;

namespace Asv.IO.Test;

public class DeviceLifetimeRegressionTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Initialize_RepeatedCallKeepsOneMicroserviceGeneration(bool asyncDispose)
    {
        using var connection = new DeviceLifetimeConnection();
        var time = new DeviceLifetimeTimeProvider();
        var device = CreateDevice(connection, time);
        device.Initialize();
        var first = Assert.Single(device.Microservices);

        device.Initialize();

        Assert.Same(first, Assert.Single(device.Microservices));
        if (asyncDispose)
        {
            await device.DisposeAsync();
        }
        else
        {
            device.Dispose();
        }
        Assert.True(Assert.IsAssignableFrom<AsyncDisposableOnce>(first).IsDisposed);
        Assert.Empty(device.Microservices);
        Assert.Equal(0, time.ActiveTimers);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplorerDispose_ClearsBothDeviceCollections(bool asyncDispose)
    {
        using var connection = new DeviceLifetimeConnection();
        var time = new DeviceLifetimeTimeProvider();
        var explorer = DeviceExplorer.Create(
            connection,
            builder =>
            {
                builder.SetTimeProvider(time);
                builder.Factories.RegisterExampleDevice(new ExampleDeviceConfig());
            }
        );
        connection.Receive(new ExampleMessage1 { SenderId = 7 });
        var initialized = explorer.InitializedDevices;
        var device = Assert.Single(initialized);

        if (asyncDispose)
        {
            await explorer.DisposeAsync();
        }
        else
        {
            explorer.Dispose();
        }

        Assert.Empty(initialized);
        Assert.Empty(explorer.Devices);
        Assert.True(Assert.IsAssignableFrom<AsyncDisposableOnce>(device).IsDisposed);
        Assert.Equal(0, time.ActiveTimers);
    }

    [Fact]
    public void ExampleDeviceDispose_ReleasesConnectionSubscription()
    {
        using var connection = new DeviceLifetimeConnection();
        var time = new DeviceLifetimeTimeProvider();
        var weak = CreateAndDisposeDevice(connection, time);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(weak.IsAlive);
        Assert.Equal(0, time.ActiveTimers);
        connection.Receive(new ExampleMessage1());
        GC.KeepAlive(connection);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndDisposeDevice(
        DeviceLifetimeConnection connection,
        DeviceLifetimeTimeProvider time
    )
    {
        var device = CreateDevice(connection, time);
        var weak = new WeakReference(device);
        device.Dispose();
        return weak;
    }

    private static ExampleDevice CreateDevice(
        DeviceLifetimeConnection connection,
        TimeProvider time
    ) =>
        new(
            new ExampleDeviceId(ExampleDevice.DeviceClass, 1),
            new ExampleDeviceConfig(),
            [],
            new MicroserviceContext(connection, timeProvider: time)
        );
}
