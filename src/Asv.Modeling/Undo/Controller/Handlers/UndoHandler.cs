using R3;

namespace Asv.Modeling;

public abstract class UndoHandler<TChange>(string id)
    : IUndoHandler, IDisposable
    where TChange : IChange
{
    private readonly Subject<IChange> _changes = new();
    private bool _suppressChanges;
    public string ChangeId => id;
    public Observable<IChange> Changes => _changes;

    public bool SuppressChanges
    {
        get => _suppressChanges;
        set => _suppressChanges = value;
    }

    protected void Publish(TChange change)
    {
        if (_suppressChanges) return;
        _changes.OnNext(change);
    }

    public abstract IChange Create();

    public async ValueTask Undo(IChange change, CancellationToken cancel)
    {
        try
        {
            _suppressChanges = true;
            await InternalUndo((TChange)change, cancel);
        }
        finally
        {
            _suppressChanges = false;
        }
    }

    protected abstract ValueTask InternalUndo(TChange change, CancellationToken cancel);

    public async ValueTask Redo(IChange change, CancellationToken cancel)
    {
        try
        {
            _suppressChanges = true;
            await InternalRedo((TChange)change, cancel);
        }
        finally
        {
            _suppressChanges = false;
        }
    }

    protected abstract ValueTask InternalRedo(TChange change, CancellationToken cancel);

    public void Dispose()
    {
        _changes.Dispose();
    }
}
