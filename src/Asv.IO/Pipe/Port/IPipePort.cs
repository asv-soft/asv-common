using System;
using System.Diagnostics;
using ObservableCollections;
using R3;

namespace Asv.IO;

public enum PipePortStatus
{
    Disconnected,
    InProgress,
    Connected,
    Error
}

public interface IPipePort: IDisposable, IAsyncDisposable
{
    string Id { get; }
    ReadOnlyReactiveProperty<PipeException?> Error { get; }
    ReadOnlyReactiveProperty<PipePortStatus> Status { get; }
    ReadOnlyReactiveProperty<bool> IsEnabled { get; }
    bool IsDisposed { get; }
    TagList Tags { get; }
    IReadOnlyObservableList<IPipeEndpoint> Pipes { get; }
    void Enable();
    void Disable();
}
