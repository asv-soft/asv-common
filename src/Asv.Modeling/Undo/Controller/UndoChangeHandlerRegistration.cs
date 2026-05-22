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
    private bool _suppressChanges;

    /// <inheritdoc />
    public void Publish(TChange change)
    {
        ThrowIfDisposed();
        if (_suppressChanges)
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
        try
        {
            _suppressChanges = true;
            await undo((TChange)undoChange, cancel);
        }
        finally
        {
            _suppressChanges = false;
        }
    }

    /// <inheritdoc />
    public override async ValueTask Redo(IUndoChange undoChange, CancellationToken cancel)
    {
        ThrowIfDisposed();
        try
        {
            _suppressChanges = true;
            await redo((TChange)undoChange, cancel);
        }
        finally
        {
            _suppressChanges = false;
        }
    }
}
