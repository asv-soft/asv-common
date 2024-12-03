using System;
using System.Collections.Immutable;

namespace Asv.IO;

/// <summary>
/// Used to create a device
/// </summary>
public interface IClientDeviceFactory
{
    int Order { get; }

    /// <summary>
    /// Try to identify new device from message stream
    /// </summary>
    /// <param name="message"></param>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    bool TryIdentify(IProtocolMessage message, out DeviceId? deviceId);
    
    void UpdateDevice(IClientDevice device, IProtocolMessage message);
    
    IClientDevice CreateDevice(IProtocolMessage message, DeviceId deviceId, IDeviceContext context, ImmutableArray<IClientDeviceExtender> extenders);
}

public abstract class ClientDeviceFactory<TMessageBase,TDeviceBase, TDeviceId> : IClientDeviceFactory
    where TDeviceId : DeviceId
    where TDeviceBase:IClientDevice
    where TMessageBase:IProtocolMessage
{
    public abstract int Order { get; }
    public bool TryIdentify(IProtocolMessage message, out DeviceId? deviceId)
    {
        if (message is TMessageBase msg)
        {
            deviceId = InternalTryIdentify(msg);
            return true;
        }
        deviceId = null;
        return false;
    }

    protected abstract DeviceId InternalTryIdentify(TMessageBase msg);

    public void UpdateDevice(IClientDevice device, IProtocolMessage message)
    {
        if (message is TMessageBase msg)
        {
            InternalUpdateDevice((TDeviceBase)device,msg);
            return;
        }
        throw new InvalidOperationException($"Unknown message type {message.GetType().Name}");
    }

    protected abstract void InternalUpdateDevice(TDeviceBase device, TMessageBase msg);

    public IClientDevice CreateDevice(IProtocolMessage message, DeviceId deviceId, IDeviceContext context,
        ImmutableArray<IClientDeviceExtender> extenders)
    {
        if (message is TMessageBase msg)
        {
            return InternalCreateDevice(msg, (TDeviceId)deviceId, context, extenders);
        }
        throw new InvalidOperationException($"Unknown message type {message.GetType().Name}");
    }

    protected abstract TDeviceBase InternalCreateDevice(TMessageBase msg, TDeviceId deviceId, IDeviceContext context,
        ImmutableArray<IClientDeviceExtender> extenders);

}

public interface IClientDeviceFactoryBuilder
{
    void Register(IClientDeviceFactory factory);
    void Clear();
}

