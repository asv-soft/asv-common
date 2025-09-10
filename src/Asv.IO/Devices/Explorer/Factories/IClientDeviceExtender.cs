using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

/// <summary>
/// Used to extend existing device with additional microservices or replace existing microservices
/// </summary>
public interface IClientDeviceExtender
{
    /// <summary>
    /// Allow to extend or replace microservices
    /// If you add or replace microservice, you need to Dispose it manually
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="existMicroservices"></param>
    /// <param name="cancel"></param>
    /// <returns></returns>
    Task Extend(
        DeviceId deviceId,
        ImmutableArray<IMicroserviceClient>.Builder existMicroservices,
        CancellationToken cancel
    );
}

public interface IClientDeviceExtenderBuilder
{
    void Register(IClientDeviceExtender extender);
    void Clear();
}
