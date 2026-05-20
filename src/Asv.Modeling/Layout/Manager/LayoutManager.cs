using Asv.Common;
using R3;

namespace Asv.Modeling;

public class LayoutManager<TBase> : AsyncDisposableOnceBag, ILayoutManager<TBase>
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
{
    private readonly TBase _owner;
    private readonly ILayoutStore _store;

    public LayoutManager(TBase owner, ILayoutStore store)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(store);

        _owner = owner;
        _store = store.AddTo(ref DisposableBag);
        _owner.Events.Catch<LoadLayoutEvent<TBase>>(LoadLayout).AddTo(ref DisposableBag);
        _owner.Events.Catch<SaveLayoutEvent<TBase>>(SaveLayout).AddTo(ref DisposableBag);
    }

    public ILayoutStore Store => _store;

    private ValueTask LoadLayout(TBase sender, LoadLayoutEvent<TBase> e, CancellationToken cancel)
    {
        if (e.IsHandled)
        {
            return ValueTask.CompletedTask;
        }

        var path = e.Sender.GetPathFrom<TBase, NavId>(_owner);
        e.IsLoaded = e.TryLoad(_store, new NavPath(path));
        e.IsHandled = true;
        return ValueTask.CompletedTask;
    }

    private ValueTask SaveLayout(TBase sender, SaveLayoutEvent<TBase> e, CancellationToken cancel)
    {
        if (e.IsHandled)
        {
            return ValueTask.CompletedTask;
        }

        var path = e.Sender.GetPathFrom<TBase, NavId>(_owner);
        e.Save(_store, new NavPath(path));
        e.IsHandled = true;
        return ValueTask.CompletedTask;
    }
}
