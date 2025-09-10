using System;
using ObservableCollections;

namespace Asv.IO;

public interface IDeviceExplorer : IDisposable, IAsyncDisposable
{
    IReadOnlyObservableDictionary<DeviceId, IClientDevice> Devices { get; }
    IReadOnlyObservableList<IClientDevice> InitializedDevices { get; }
}
