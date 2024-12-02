namespace Asv.IO;

/// <summary>
/// Used to create a device
/// </summary>
public interface IClientDeviceFactory
{
    int Order { get; }
    
    string DeviceClass { get; }
    /// <summary>
    /// Try to identify new device from message stream
    /// </summary>
    /// <param name="message"></param>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    bool TryIdentify(IProtocolMessage message, out string? deviceId);
    
    void UpdateDevice(IClientDevice device, IProtocolMessage message);
    
    IClientDevice CreateDevice(IProtocolMessage message, string deviceId);
}

public interface IClientDeviceFactoryBuilder
{
    void Register(IClientDeviceFactory factory);
    void Clear();
}

