# Resource lifetime and execution

`Asv.Common` provides base classes for resource ownership and helpers for executing work. Use explicit ownership: the object that creates a subscription, cancellation source, or other resource should arrange its cleanup.

## Synchronous cleanup

Derive from `DisposableOnce` and implement `InternalDisposeOnce`. The base class marks the object as disposed before calling this method and invokes it at most once. Call `ThrowIfDisposed` from operations that must reject use after disposal.

```csharp
using Asv.Common;
using R3;

using var owner = new SubscriptionOwner();
owner.Listen();

sealed class SubscriptionOwner : DisposableOnce
{
    private readonly CompositeDisposable _subscriptions = new();

    public void Listen()
    {
        ThrowIfDisposed();
        Observable.Return(42)
            .Subscribe(value => Console.WriteLine(value))
            .DisposeItWith(_subscriptions);
    }

    protected override void InternalDisposeOnce()
    {
        _subscriptions.Dispose();
    }
}
```

The guard prevents repeated cleanup. It does not serialize ordinary method calls with disposal or make a derived class thread-safe.

## Asynchronous cleanup

`AsyncDisposableOnce` implements both `IDisposable` and `IAsyncDisposable`. Its entry points share a disposed flag, but follow different cleanup paths:

| Entry point | Override called |
| --- | --- |
| `Dispose()` | `Dispose(bool disposing)` |
| `DisposeAsync()` | `DisposeAsyncCore()` |

Calling one entry point prevents the other from performing cleanup later. If a derived class supports both usage patterns, implement both paths. Use `await using` when cleanup requires asynchronous work.

`AsyncDisposableOnceBag` adds an R3 disposable bag. Cancellation-aware variants include `DisposableOnceWithCancel`, `AsyncDisposableWithCancel`, and `AsyncDisposableOnceBagWithCancel`. Their protected members differ; inspect the chosen base class before adding an override. A disposal cancellation token signals shutdown; owners must still observe completion of work that they start.

## Execution helpers

`LockByKeyExecutor<TKey>` executes synchronous callbacks under a lock associated with a key. Calls using the same executor and equal keys are serialized; different keys can execute concurrently.

```csharp
using Asv.Common;

var executor = new LockByKeyExecutor<string>(StringComparer.Ordinal);
var result = executor.Execute("device-1", () => 6 * 7);
Console.WriteLine(result); // 42
```

Share the executor between callers that need coordination. Do not pass an asynchronous callback to the `Action` overload: it would become `async void`, and the lock would not cover its asynchronous completion. Recursive execution for the same key is unsupported.

`SafeFireAndForget` accepts `Task` and `ValueTask`, with optional exception handlers. Without a matching local or default exception handler, a failure can escape its `async void` execution path. Prefer awaiting work whose completion or failure affects the caller. Its default handler and rethrow setting are shared static state.
