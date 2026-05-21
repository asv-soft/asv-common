using Asv.Common;
using R3;

namespace Asv.Modeling;

/// <summary>
/// Routes layout load and save events to an <see cref="ILayoutStore"/>.
/// </summary>
/// <typeparam name="TBase">The routed event and navigation base type.</typeparam>
public class LayoutManager<TBase> : AsyncDisposableOnceBag, ILayoutManager<TBase>
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
{
    private readonly TBase _owner;
    private readonly ILayoutStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutManager{TBase}"/> class.
    /// </summary>
    /// <param name="owner">The root object used to calculate navigation paths.</param>
    /// <param name="store">The store used to persist layout values.</param>
    public LayoutManager(TBase owner, ILayoutStore store)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(store);

        _owner = owner;
        _store = store.AddTo(ref DisposableBag);
        _owner.Events.Catch<LoadLayoutEvent<TBase>>(LoadLayout).AddTo(ref DisposableBag);
        _owner.Events.Catch<SaveLayoutEvent<TBase>>(SaveLayout).AddTo(ref DisposableBag);
    }

    /// <inheritdoc />
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
