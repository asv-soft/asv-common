using ObservableCollections;

namespace Asv.IO;

public interface IClientDeviceBrowser
{
    IReadOnlyObservableDictionary<DeviceId,IClientDevice> Devices { get; }
}