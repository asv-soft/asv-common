using System;
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
public interface IProtocolPort:IDisposable,IAsyncDisposable,IProtocolMessagePipe
{
    string Id { get; }
    ReadOnlyReactiveProperty<ProtocolException?> Error { get; }
    ReadOnlyReactiveProperty<ProtocolPortStatus> Status { get; }
    ReadOnlyReactiveProperty<bool> IsEnabled { get; }
    TagList Tags { get; }
    IReadOnlyObservableList<IProtocolConnection> Connections { get; }
    void Enable();
    void Disable();
    bool IsDisposed { get; }
}