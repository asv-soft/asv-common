using System;
using System.Diagnostics.CodeAnalysis;
using ObservableCollections;

namespace Asv.IO;

public interface IDeviceExplorer : IDisposable, IAsyncDisposable
{
    /// <summary>Identifies a message without creating or updating a device.</summary>
    bool TryIdentify(IProtocolMessage message, [NotNullWhen(true)] out DeviceId? deviceId);

    IReadOnlyObservableDictionary<DeviceId, IClientDevice> Devices { get; }
    IReadOnlyObservableList<IClientDevice> InitializedDevices { get; }
}
