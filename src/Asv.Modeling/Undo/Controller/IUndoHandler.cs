using R3;

namespace Asv.Modeling;

public interface IUndoHandler
{
    IChange Create();
    ValueTask Undo(IChange change, CancellationToken cancel);
    ValueTask Redo(IChange change, CancellationToken cancel);
}
