using R3;

namespace Asv.Modeling;

public abstract class UndoChangeHandler<TChange>(string id, Observable<IChange> changes)
    : IUndoHandler
{
    private bool _muteChanges;
    public string ChangeId => id;
    public Observable<IChange> Changes => changes.Where(_ => !_muteChanges);

    public bool MuteChanges
    {
        get => _muteChanges;
        set => _muteChanges = value;
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
}
