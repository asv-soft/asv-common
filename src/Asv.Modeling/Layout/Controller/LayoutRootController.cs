using Asv.Common;
using R3;

namespace Asv.Modeling;

public sealed class LayoutRootController<TBase> : AsyncDisposableOnceBag
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
{
    private readonly TBase _owner;
    private readonly ILayoutStore _store;

    public LayoutRootController(TBase owner, ILayoutStore store)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(store);

        _owner = owner;
        _store = store.AddTo(ref DisposableBag);
        _owner.Events.Catch<LoadLayoutEvent<TBase>>(LoadLayout).AddTo(ref DisposableBag);
        _owner.Events.Catch<SaveLayoutEvent<TBase>>(SaveLayout).AddTo(ref DisposableBag);
    }

    public ILayoutStore Store => _store;

    public async ValueTask LoadAsync(CancellationToken cancel = default)
    {
        await ForEachLayout(_owner, static (layout, token) => layout.LoadAsync(token), cancel)
            .ConfigureAwait(false);
    }

    public async ValueTask SaveAsync(CancellationToken cancel = default)
    {
        await ForEachLayout(_owner, static (layout, token) => layout.SaveAsync(token), cancel)
            .ConfigureAwait(false);
    }

    private ValueTask LoadLayout(TBase sender, LoadLayoutEvent<TBase> e, CancellationToken cancel)
    {
        var path = e.Sender.GetPathFrom<TBase, NavId>(_owner);
        e.IsLoaded = _store.Load(new NavPath(path), e.LayoutId, e.LayoutData);
        e.IsHandled = true;
        return ValueTask.CompletedTask;
    }

    private ValueTask SaveLayout(TBase sender, SaveLayoutEvent<TBase> e, CancellationToken cancel)
    {
        var path = e.Sender.GetPathFrom<TBase, NavId>(_owner);
        _store.Save(new NavPath(path), e.LayoutId, e.LayoutData);
        e.IsHandled = true;
        return ValueTask.CompletedTask;
    }

    private static async ValueTask ForEachLayout(
        TBase current,
        Func<ILayoutController, CancellationToken, ValueTask> action,
        CancellationToken cancel
    )
    {
        cancel.ThrowIfCancellationRequested();

        if (current is ISupportLayout layoutOwner)
        {
            await action(layoutOwner.Layout, cancel).ConfigureAwait(false);
        }

        foreach (var child in current.GetChildren())
        {
            await ForEachLayout(child, action, cancel).ConfigureAwait(false);
        }
    }
}
