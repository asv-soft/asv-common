using System;
using System.Collections.Immutable;
using R3;

namespace Asv.IO;

public interface IProtocolRouter : IProtocolConnection
{
    ImmutableArray<ProtocolInfo> AvailableProtocols { get; }
    ImmutableArray<PortTypeInfo> AvailablePortTypes { get; }
    ImmutableArray<IProtocolPort> Ports { get; }
    Observable<IProtocolPort> PortAdded { get; }
    Observable<IProtocolPort> PortRemoved { get; }
    Observable<IProtocolPort> PortUpdated { get; }
    IProtocolPort AddPort(Uri connectionString);
    void RemovePort(IProtocolPort port);
}

public static class ProtocolRouterHelper
{
    public static IProtocolPort AddPort(this IProtocolRouter src, string connectionString)
    {
        return src.AddPort(new Uri(connectionString));
    }
}
