using System.Collections.Immutable;
using Asv.IO.Device;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace Asv.IO.Test.Devices.Client;

[TestSubject(typeof(ClientDeviceHelper))]
public class ClientDeviceHelperTest
{
    [Fact]
    public void GetRequiredMicroservice_ExistMicroservice_ReturnsMicroservice()
    {
        // Arrange
        using var link = CreateLink();
        using var device = CreateDevice(link.Client);
        device.Initialize();

        // Act
        var result = device.GetRequiredMicroservice<ExampleDeviceMicroservice>();

        // Assert
        Assert.Same(device.Microservices[0], result);
    }

    [Fact]
    public void GetRequiredMicroservice_MissingMicroservice_ThrowsException()
    {
        // Arrange
        using var link = CreateLink();
        using var device = CreateDevice(link.Client);
        device.Initialize();

        // Act
        var exception = Assert.Throws<ClientDeviceMicroserviceNotFoundException>(
            device.GetRequiredMicroservice<IMicroserviceServer>
        );

        // Assert
        Assert.Same(device, exception.Device);
        exception.MicroserviceType.Should().Be<IMicroserviceServer>();
    }

    private static IVirtualConnection CreateLink()
    {
        var protocol = Protocol.Create(builder =>
        {
            builder.Protocols.RegisterExampleProtocol();
        });

        return protocol.CreateVirtualConnection();
    }

    private static ExampleDevice CreateDevice(IProtocolConnection connection)
    {
        return new ExampleDevice(
            new ExampleDeviceId(ExampleDevice.DeviceClass, 1),
            new ExampleDeviceConfig(),
            ImmutableArray<IClientDeviceExtender>.Empty,
            new MicroserviceContext(connection)
        );
    }
}
