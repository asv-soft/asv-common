using R3;

namespace Asv.Modeling;

public abstract class UndoHandler<TChange>(string id)
    : IUndoHandler, IDisposable
    where TChange : IChange
{
    private readonly Subject<IChange> _changes = new();
    private bool _muteChanges;
    public string ChangeId => id;
    public Observable<IChange> Changes => _changes;

    public bool MuteChanges
    {
        get => _muteChanges;
        set => _muteChanges = value;
    }

    protected void Publish(TChange change)
    {
        if (_muteChanges) return;
        _changes.OnNext(change);
    }

    public abstract IChange Create();

    public async ValueTask Undo(IChange change, CancellationToken cancel)
    {
        try
        {
            _muteChanges = true;
            await InternalUndo((TChange)change, cancel);
        }
        finally
        {
            _muteChanges = false;
        }
    }

    protected abstract ValueTask InternalUndo(TChange change, CancellationToken cancel);

    public async ValueTask Redo(IChange change, CancellationToken cancel)
    {
        try
        {
            _muteChanges = true;
            await InternalRedo((TChange)change, cancel);
        }
        finally
        {
            _muteChanges = false;
        }
    }

    protected abstract ValueTask InternalRedo(TChange change, CancellationToken cancel);

    public void Dispose()
    {
        _changes.Dispose();
    }
}
