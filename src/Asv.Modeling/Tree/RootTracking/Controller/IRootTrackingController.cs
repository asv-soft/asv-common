using R3;

namespace Asv.Modeling;

public interface IRootTrackingController<TRoot>
{
    ReadOnlyReactiveProperty<TRoot?> Root { get; }
    public Observable<TRoot> Attached => Root.Where(x => x != null).Select(x => x!);
    public Observable<Unit> Detached => Root.Where(x => x == null).Select(_ => Unit.Default);
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
}
