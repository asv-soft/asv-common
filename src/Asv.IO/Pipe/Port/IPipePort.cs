using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
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
    IPipeEndpoint[] Pipes { get; }
    Observable<IPipeEndpoint[]> OnEndpointsChanged { get; }
    void Enable();
    void Disable();
}

public interface IPipeEndpoint : IDuplexPipe, IDisposable,IAsyncDisposable
{
    string Id { get; }
    IPipePort Parent { get; }
    bool IsDisposed { get; }
    TagList Tags { get; }
}

public static class PipePortExtensions
{
    public static async Task WaitConnected(this IPipePort port,CancellationToken cancel)
    {
        var tcs = new TaskCompletionSource();
        await using var c1 = cancel.Register(()=>tcs.TrySetCanceled());
        using var c2 = port.Status.Where(x => x == PipePortStatus.Connected).Take(1).Subscribe(x => tcs.TrySetResult());
        await tcs.Task;
    }
    public static Task WaitConnected(this IPipePort port,TimeSpan timeout)
    {
        using var cancel = new CancellationTokenSource(timeout);
        return port.WaitConnected(cancel.Token);
    }
    public static Task Enable(this IPipePort port,CancellationToken cancel)
    {
        return Task.Factory.StartNew(port.Enable, cancel);
    }
    public static Task Disable(this IPipePort port,CancellationToken cancel)
    {
        return Task.Factory.StartNew(port.Disable, cancel);
    }
}
