using System;
using System.Diagnostics;
using R3;

namespace Asv.IO;

public enum ProtocolPortStatus
{
    Disconnected,
    InProgress,
    Connected,
    Error
}
public interface IProtocolPort:IDisposable,IAsyncDisposable
{
    string Id { get; }
    ReadOnlyReactiveProperty<ProtocolException?> Error { get; }
    ReadOnlyReactiveProperty<ProtocolPortStatus> Status { get; }
    ReadOnlyReactiveProperty<bool> IsEnabled { get; }
    TagList Tags { get; }
    IProtocolConnection[] Connections { get; }
    Observable<IProtocolConnection[]> OnConnectionsChanged { get; }
    void Enable();
    void Disable();
    bool IsDisposed { get; }
}