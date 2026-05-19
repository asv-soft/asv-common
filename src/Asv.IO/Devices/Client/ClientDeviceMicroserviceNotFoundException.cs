using System;

namespace Asv.IO;

public class ClientDeviceMicroserviceNotFoundException : InvalidOperationException
{
    public ClientDeviceMicroserviceNotFoundException() { }

    public ClientDeviceMicroserviceNotFoundException(string message)
        : base(message) { }

    public ClientDeviceMicroserviceNotFoundException(string message, Exception innerException)
        : base(message, innerException) { }

    public ClientDeviceMicroserviceNotFoundException(IClientDevice device, Type microserviceType)
        : base(CreateMessage(device, microserviceType))
    {
        Device = device;
        MicroserviceType = microserviceType;
    }

    public IClientDevice? Device { get; }

    public Type? MicroserviceType { get; }

    private static string CreateMessage(IClientDevice device, Type microserviceType)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(microserviceType);

        return $"Required microservice '{microserviceType.FullName}' was not found in device '{device.Id}'.";
    }
}
