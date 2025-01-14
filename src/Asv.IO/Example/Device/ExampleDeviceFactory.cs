using System;
using System.Collections.Immutable;

namespace Asv.IO.Device;


public class ExampleDeviceFactory(ExampleDeviceConfig config) : ClientDeviceFactory<ExampleMessageBase,ExampleDevice,ExampleDeviceId>
{
    public override int Order { get; } = 0;

    protected override bool InternalTryIdentify(ExampleMessageBase msg, out ExampleDeviceId? deviceId)
    {
        deviceId = new ExampleDeviceId(ExampleDevice.DeviceClass, msg.SenderId);
        return true;
    }

    protected override void InternalUpdateDevice(ExampleDevice device, ExampleMessageBase msg)
    {
        // nothing to do
    }

    protected override ExampleDevice InternalCreateDevice(ExampleMessageBase msg, ExampleDeviceId deviceId, IMicroserviceContext context,
        ImmutableArray<IClientDeviceExtender> extenders)
    {
        return new ExampleDevice(deviceId, config, extenders, context);
    }
}