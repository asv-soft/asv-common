using R3;

namespace Asv.Modeling;

public interface IUndoHandler
{
    string ChangeId { get; }
    Observable<IChange> Changes { get; }
    bool SuppressChanges { get; set; }
    IChange Create();
    ValueTask Undo(IChange change, CancellationToken cancel);
    ValueTask Redo(IChange change, CancellationToken cancel);
}
