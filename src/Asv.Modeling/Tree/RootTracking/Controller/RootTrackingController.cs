using Asv.Common;
using R3;

namespace Asv.Modeling;

/// <summary>
/// Default implementation of <see cref="IRootTrackingController{TRoot}"/>.
/// </summary>
/// <typeparam name="TBase">The tree node type.</typeparam>
/// <typeparam name="TRoot">The root node type tracked by this controller.</typeparam>
public class RootTrackingController<TBase, TRoot>
    : AsyncDisposableOnceBag,
        IRootTrackingController<TRoot>
    where TBase : ISupportParentChange<TBase>,
        ISupportRoutedEvents<TBase>,
        ISupportRootTracking<TBase, TRoot>
    where TRoot : TBase
{
    private readonly TBase _owner;
    private readonly ReactiveProperty<TRoot?> _root;

    /// <summary>
    /// Initializes a new instance of the <see cref="RootTrackingController{TBase, TRoot}"/> class.
    /// </summary>
    /// <param name="owner">The owner node whose root attachment is tracked.</param>
    public RootTrackingController(TBase owner)
    {
        _owner = owner;
        _root = new ReactiveProperty<TRoot?>(default).AddTo(ref DisposableBag);
        owner
            .ParentChanged.SubscribeAwait(ParentChanged, AwaitOperation.ThrottleFirstLast)
            .AddTo(ref DisposableBag);
        owner
            .Events.Catch<RootAttachedEvent<TBase, TRoot>>(x => _root.Value = x.Root)
            .AddTo(ref DisposableBag);
        owner
            .Events.Catch<RootDetachedEvent<TBase>>(_ => _root.Value = default)
            .AddTo(ref DisposableBag);
        ParentChanged(owner.Parent, CancellationToken.None).SafeFireAndForget();
    }

    private ValueTask ParentChanged(TBase? parent, CancellationToken cancel)
    {
        if (_owner is TRoot selfIsRoot)
        {
            if (parent != null)
            {
                throw new InvalidOperationException(
                    "Owner already has parent, but it is also root"
                );
            }
            return _owner.Rise(new RootAttachedEvent<TBase, TRoot>(_owner, selfIsRoot), cancel);
        }
        if (parent is TRoot parentIsRoot)
        {
            return _owner.Rise(new RootAttachedEvent<TBase, TRoot>(_owner, parentIsRoot), cancel);
        }
        if (
            parent is ISupportRootTracking<TBase, TRoot>
            {
                RootTracking.Root.CurrentValue: { } existRoot
            }
        )
        {
            return _owner.Rise(new RootAttachedEvent<TBase, TRoot>(_owner, existRoot), cancel);
        }
        return _owner.Rise(new RootDetachedEvent<TBase>(_owner), cancel);
    }

    /// <inheritdoc />
    public ReadOnlyReactiveProperty<TRoot?> Root => _root;
}
