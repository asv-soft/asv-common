namespace Asv.IO.Device;

public static class ExampleDeviceHelper
{
    public static void RegisterExampleDevice(this IClientDeviceFactoryBuilder builder, ExampleDeviceConfig config)
    {
        builder.Register(new ExampleDeviceFactory(config));
    }
}