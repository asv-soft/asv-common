using Asv.Common;

namespace Asv.Modeling;

internal abstract class LayoutRegistration(string id, Action<string> remove)
    : AsyncDisposableOnce,
        ILayoutRegistration
{
    public string Id => id;

    public abstract ILayoutData Create();

    public abstract ValueTask LoadAsync(CancellationToken cancel = default);

    public abstract ValueTask SaveAsync(CancellationToken cancel = default);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            remove(id);
        }

        base.Dispose(disposing);
    }

    protected override ValueTask DisposeAsyncCore()
    {
        remove(id);
        return base.DisposeAsyncCore();
    }
}

internal sealed class LayoutRegistration<TBase, TData>(
    TBase owner,
    string id,
    AsyncLoadLayoutCallback<TData> load,
    AsyncSaveLayoutCallback<TData> save,
    Func<TData> factory,
    Action<string> remove
) : LayoutRegistration(id, remove)
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
    where TData : ILayoutData
{
    public override ILayoutData Create()
    {
        ThrowIfDisposed();
        return factory();
    }

    public override async ValueTask LoadAsync(CancellationToken cancel = default)
    {
        ThrowIfDisposed();
        var data = factory();
        var loadEvent = new LoadLayoutEvent<TBase>(owner, data, Id);
        await owner.Rise(loadEvent, cancel).ConfigureAwait(false);
        if (loadEvent.IsLoaded)
        {
            await load(data, cancel).ConfigureAwait(false);
        }
    }

    public override async ValueTask SaveAsync(CancellationToken cancel = default)
    {
        ThrowIfDisposed();
        var data = factory();
        await save(data, cancel).ConfigureAwait(false);
        await owner.Rise(new SaveLayoutEvent<TBase>(owner, data, Id), cancel).ConfigureAwait(false);
    }
}
