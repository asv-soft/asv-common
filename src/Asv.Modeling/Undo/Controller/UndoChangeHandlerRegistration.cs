using Asv.Common;
using R3;

namespace Asv.Modeling;

internal abstract class UndoChangeHandlerRegistration(string id, Action<string> remove)
    : AsyncDisposableOnce,
        IUndoChangeHandler
{
    protected string Id => id;

    /// <inheritdoc />
    public abstract IUndoChange Create();

    /// <inheritdoc />
    public abstract ValueTask Undo(IUndoChange undoChange, CancellationToken cancel);

    /// <inheritdoc />
    public abstract ValueTask Redo(IUndoChange undoChange, CancellationToken cancel);

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

internal sealed class UndoChangeHandlerRegistration<TChange>(
    string id,
    AsyncUndoCallback<TChange> undo,
    AsyncUndoCallback<TChange> redo,
    Func<TChange> factory,
    Subject<(string, IUndoChange)> changes,
    Action<string> remove
) : UndoChangeHandlerRegistration(id, remove), IUndoChangeSink<TChange>
    where TChange : IUndoChange
{
    private int _suppressChangePublicationCount;

    private bool IsChangePublicationSuppressed =>
        Volatile.Read(ref _suppressChangePublicationCount) > 0;

    /// <inheritdoc />
    public IDisposable SuppressChangePublication()
    {
        Interlocked.Increment(ref _suppressChangePublicationCount);
        return Disposable.Create(
            this,
            static sink => Interlocked.Decrement(ref sink._suppressChangePublicationCount)
        );
    }

    /// <inheritdoc />
    public void Publish(TChange change)
    {
        ThrowIfDisposed();
        if (IsChangePublicationSuppressed)
            return;
        changes.OnNext((Id, change));
    }

    /// <inheritdoc />
    public override IUndoChange Create()
    {
        ThrowIfDisposed();
        return factory();
    }

    /// <inheritdoc />
    public override async ValueTask Undo(IUndoChange undoChange, CancellationToken cancel)
    {
        ThrowIfDisposed();
        using (SuppressChangePublication())
        {
            await undo((TChange)undoChange, cancel);
        }
    }

    /// <inheritdoc />
    public override async ValueTask Redo(IUndoChange undoChange, CancellationToken cancel)
    {
        ThrowIfDisposed();
        using (SuppressChangePublication())
        {
            await redo((TChange)undoChange, cancel);
        }
    }
}
