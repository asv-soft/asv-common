using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Asv.Common;

public abstract class UndoChangeHandler<TChange>(string id, Observable<IChange> changes)
    : IUndoHandler
{
    private bool _disableChanges = false;
    public string RegistrationId => id;
    public Observable<IChange> Changes => changes.Where(_ => !_disableChanges);
    public abstract IChange Create();

    public ValueTask Undo(IChange change, CancellationToken cancel)
    {
        try
        {
            _disableChanges = true;
            return InternalUndo((TChange)change, cancel);
        }
        finally
        {
            _disableChanges = false;
        }
    }

    protected abstract ValueTask InternalUndo(TChange change, CancellationToken cancel);

    public ValueTask Redo(IChange change, CancellationToken cancel)
    {
        try
        {
            _disableChanges = true;
            return InternalRedo((TChange)change, cancel);
        }
        finally
        {
            _disableChanges = false;
        }
    }

    protected abstract ValueTask InternalRedo(TChange change, CancellationToken cancel);
}
