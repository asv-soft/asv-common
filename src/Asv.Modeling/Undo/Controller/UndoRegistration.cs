using Asv.Common;
using R3;

namespace Asv.Modeling;

public abstract class UndoChangeRegistration(string id, Action<string> remove)
    : AsyncDisposableOnce,
        IUndoChangeHandler
{
    protected string Id => id;

    public abstract IUndoChange Create();

    public abstract ValueTask Undo(IUndoChange undoChange, CancellationToken cancel);

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

public sealed class UndoChangeRegistration<TChange>(
    string id,
    UndoCallback<TChange> undo,
    UndoCallback<TChange> redo,
    Func<TChange> factory,
    Subject<(string, IUndoChange)> changes,
    Action<string> remove
) : UndoChangeRegistration(id, remove), IUndoChangeSink<TChange>
    where TChange : IUndoChange
{
    private bool _suppressChanges;

    public void Publish(TChange change)
    {
        ThrowIfDisposed();
        if (_suppressChanges)
            return;
        changes.OnNext((Id, change));
    }

    public override IUndoChange Create()
    {
        ThrowIfDisposed();
        return factory();
    }

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
