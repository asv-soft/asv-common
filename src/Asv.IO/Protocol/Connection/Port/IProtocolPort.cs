using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using ObservableCollections;
using R3;

namespace Asv.IO;

public enum ProtocolPortStatus
{
    Disconnected,
    InProgress,
    Connected,
    Error
}
public interface IProtocolPort:IDisposable,IAsyncDisposable,IProtocolConnection
{
    PortTypeInfo TypeInfo { get; }
    IEnumerable<ProtocolInfo> Protocols { get; }
    ReadOnlyReactiveProperty<ProtocolException?> Error { get; }
    ReadOnlyReactiveProperty<ProtocolPortStatus> Status { get; }
    ReadOnlyReactiveProperty<bool> IsEnabled { get; }
    ImmutableArray<IProtocolEndpoint> Endpoints { get; }
    Observable<IProtocolEndpoint> EndpointAdded { get; }
    Observable<IProtocolEndpoint> EndpointRemoved { get; }
    void Enable();
    void Disable();
    bool IsDisposed { get; }
}

public class PortTypeInfo(string scheme, string name)
{
    public string Scheme { get; set; } = scheme;
    public string Name { get; set; } = name;
    public override string ToString()
    {
        return $"{Scheme}[{Name}]";
    }
}