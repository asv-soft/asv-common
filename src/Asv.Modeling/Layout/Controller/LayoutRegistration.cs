using Asv.Common;

namespace Asv.Modeling;

internal abstract class LayoutRegistration(string id, Action<string> remove) : AsyncDisposableOnce
{
    public string Id => id;

    public abstract ValueTask LoadAsync(CancellationToken cancel = default);

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
    LoadLayoutCallback<TData> loadLayout,
    Action<string> remove
) : LayoutRegistration(id, remove), ILayoutSink<TData>
    where TBase : ISupportRoutedEvents<TBase>
{
    public override async ValueTask LoadAsync(CancellationToken cancel = default)
    {
        ThrowIfDisposed();
        var loadEvent = new LoadLayoutEvent<TBase, TData>(owner, Id);
        await owner.Rise(loadEvent, cancel).ConfigureAwait(false);
        if (loadEvent.IsLoaded)
        {
            await loadLayout(loadEvent.LayoutData, cancel).ConfigureAwait(false);
        }
    }

    public ValueTask SaveAsync(TData data, CancellationToken cancel = default)
    {
        ThrowIfDisposed();
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        return owner.Rise(new SaveLayoutEvent<TBase, TData>(owner, data, Id), cancel);
    }
}
