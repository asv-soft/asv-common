using ObservableCollections;

namespace Asv.IO;

public interface IClientDeviceBrowser
{
    IReadOnlyObservableDictionary<string,IClientDevice> Devices { get; }
}