using R3;

namespace Asv.Modeling;

public class CallbackUndoHandler<TChange>(
    string id,
    CallbackUndoHandler<TChange>.Delegate undo,
    CallbackUndoHandler<TChange>.Delegate redo,
    Observable<TChange> publicator)
    : UndoHandler<TChange>(id, publicator.Cast<TChange, IChange>())
    where TChange : IChange, new()
{
    public delegate ValueTask Delegate(TChange change, CancellationToken cancel);

    public override IChange Create() => new TChange();
    protected override ValueTask InternalUndo(TChange change, CancellationToken cancel) => undo(change, cancel);
    protected override ValueTask InternalRedo(TChange change, CancellationToken cancel) => redo(change, cancel);
}