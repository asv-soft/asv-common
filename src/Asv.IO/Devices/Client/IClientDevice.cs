using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using R3;

namespace Asv.IO;

public interface IClientDevice:IDisposable, IAsyncDisposable
{
    DeviceId Id { get; }
    ReadOnlyReactiveProperty<string?> Name { get; }
    ReadOnlyReactiveProperty<ClientDeviceState> State { get; }
    ILinkIndicator Link { get; }
    ImmutableArray<IMicroserviceClient> Microservices { get; }
}

public static class ClientDeviceHelper
{
    public static async Task WaitUntilConnect(this IClientDevice src, int timeoutMs, TimeProvider timeProvider)
    {
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs),timeProvider);
        var tcs = new TaskCompletionSource();
        cancel.Token.Register(() => tcs.TrySetCanceled());
        using var c = src.Link.State.Where(s => s == LinkState.Connected)
            .Subscribe(x => tcs.TrySetResult());
        await tcs.Task.ConfigureAwait(false);
    }
    
    public static async Task WaitUntilConnectAndInit(this IClientDevice src, int timeoutMs, TimeProvider timeProvider)
    {
        await src.WaitUntilConnect(timeoutMs,timeProvider).ConfigureAwait(false);
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs),timeProvider);
        var tcs = new TaskCompletionSource();
        cancel.Token.Register(() => tcs.TrySetCanceled());
        using var c = src.State.Where(s => s == ClientDeviceState.Complete)
            .Subscribe(x => tcs.TrySetResult());
        await tcs.Task.ConfigureAwait(false);
    }
}

public enum ClientDeviceState
{
    /// <summary>
    /// Represent 
    /// </summary>
    Uninitialized,

    /// <summary>
    /// Represents the state of an initialization process where the initialization has failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Represents the current initialization state as being in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Represents the initialization state of a process.
    /// </summary>
    Complete
}