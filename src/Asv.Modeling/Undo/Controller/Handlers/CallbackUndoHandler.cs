using R3;

namespace Asv.Modeling;

public class ManualUndoHandler<TChange>(
    string id,
    ManualUndoHandler<TChange>.Delegate undo,
    ManualUndoHandler<TChange>.Delegate redo)
    : UndoHandler<TChange>(id)
    where TChange : IChange, new()
{
    public new void Publish(TChange change)
    {
        base.Publish(change);
    }
    public delegate ValueTask Delegate(TChange change, CancellationToken cancel);
    public override IChange Create() => new TChange();
    protected override ValueTask InternalUndo(TChange change, CancellationToken cancel) => undo(change, cancel);
    protected override ValueTask InternalRedo(TChange change, CancellationToken cancel) => redo(change, cancel);
}