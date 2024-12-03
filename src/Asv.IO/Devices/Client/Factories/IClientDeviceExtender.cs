using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

/// <summary>
/// Used to extend existing device with additional microservices or replace existing microservices
/// </summary>
public interface IClientDeviceExtender
{
    
    Task Extend(DeviceId deviceId, ImmutableArray<IMicroserviceClient>.Builder existMicroservices, CancellationToken cancel);
}

public interface IClientDeviceExtenderBuilder
{
    void Register(IClientDeviceExtender extender);
    void Clear();
}