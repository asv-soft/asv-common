using Asv.Common;
using R3;

namespace Asv.Modeling;

public abstract class UndoRegistration(string id, Action<string> remove)
    : AsyncDisposableOnce,
        IUndoHandler
{
    protected string Id => id;

    public abstract IChange Create();

    public abstract ValueTask Undo(IChange change, CancellationToken cancel);

    public abstract ValueTask Redo(IChange change, CancellationToken cancel);

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

public sealed class UndoRegistration<TChange>(
    string id,
    UndoCallback<TChange> undo,
    UndoCallback<TChange> redo,
    Func<TChange> factory,
    Subject<(string, IChange)> changes,
    Action<string> remove
) : UndoRegistration(id, remove), IUndoPublisher<TChange>
    where TChange : IChange
{
    private bool _suppressChanges;

    public void Publish(TChange change)
    {
        ThrowIfDisposed();
        if (_suppressChanges)
            return;
        changes.OnNext((Id, change));
    }

    public override IChange Create()
    {
        ThrowIfDisposed();
        return factory();
    }

    public override async ValueTask Undo(IChange change, CancellationToken cancel)
    {
        ThrowIfDisposed();
        try
        {
            _suppressChanges = true;
            await undo((TChange)change, cancel);
        }
        finally
        {
            _suppressChanges = false;
        }
    }

    public override async ValueTask Redo(IChange change, CancellationToken cancel)
    {
        ThrowIfDisposed();
        try
        {
            _suppressChanges = true;
            await redo((TChange)change, cancel);
        }
        finally
        {
            _suppressChanges = false;
        }
    }
}
