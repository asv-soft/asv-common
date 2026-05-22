using Asv.Common;
using R3;

namespace Asv.Modeling;

/// <summary>
/// Tracks the root object that the owner is currently attached to.
/// </summary>
/// <typeparam name="TRoot">The root object type.</typeparam>
public interface IRootTrackingController<TRoot>
{
    /// <summary>
    /// Gets the currently attached root, or <see langword="null"/> when the owner is detached.
    /// </summary>
    ReadOnlyReactiveProperty<TRoot?> Root { get; }

    /// <summary>
    /// Gets an observable sequence that publishes each non-null root attachment.
    /// </summary>
    public Observable<TRoot> Attached => Root.Where(x => x != null).Select(x => x!);

    /// <summary>
    /// Gets an observable sequence that publishes when the current root is cleared.
    /// </summary>
    public Observable<Unit> Detached => Root.Where(x => x == null).Select(_ => Unit.Default);

    /// <summary>
    /// Executes the specified action immediately if a root is already attached, and again every time a root is attached.
    /// </summary>
    /// <param name="action">The action to execute with the attached root.</param>
    /// <returns>A disposable subscription that stops future executions when disposed.</returns>
    public IDisposable ExecuteWhenRootAttached(Action<TRoot> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (Root.CurrentValue is { } root)
        {
            action(root);
            var isFirstNotification = true;
            return Root.Subscribe(next =>
            {
                if (isFirstNotification)
                {
                    isFirstNotification = false;
                    if (ReferenceEquals(next, root))
                    {
                        return;
                    }
                }

                if (next is { } attachedRoot)
                {
                    action(attachedRoot);
                }
            });
        }

        return Attached.Subscribe(action);
    }

    /// <summary>
    /// Executes the specified asynchronous action immediately if a root is already attached,
    /// and again every time a root is attached.
    /// </summary>
    /// <param name="action">The asynchronous action to execute with the attached root.</param>
    /// <returns>A disposable subscription that stops future executions when disposed.</returns>
    public IDisposable ExecuteWhenRootAttached(Func<TRoot, ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return ExecuteWhenRootAttached((root, _) => action(root));
    }

    /// <summary>
    /// Executes the specified asynchronous action immediately if a root is already attached,
    /// and again every time a root is attached.
    /// </summary>
    /// <param name="action">The asynchronous action to execute with the attached root and cancellation token.</param>
    /// <returns>A disposable subscription that stops future executions when disposed.</returns>
    public IDisposable ExecuteWhenRootAttached(Func<TRoot, CancellationToken, ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (Root.CurrentValue is { } root)
        {
            action(root, default).SafeFireAndForget();
            var isFirstNotification = true;
            return Root.SubscribeAwait(
                (next, cancel) =>
                {
                    if (isFirstNotification)
                    {
                        isFirstNotification = false;
                        if (ReferenceEquals(next, root))
                        {
                            return ValueTask.CompletedTask;
                        }
                    }

                    if (next is { } attachedRoot)
                    {
                        return action(attachedRoot, cancel);
                    }

                    return ValueTask.CompletedTask;
                }
            );
        }

        return Attached.SubscribeAwait(action);
    }
}
