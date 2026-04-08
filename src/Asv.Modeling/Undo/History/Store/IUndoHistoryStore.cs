namespace Asv.Modeling;

public interface IUndoHistoryStore<TId> : IDisposable
{
    void LoadUndoRedo(Action<IUndoSnapshot<TId>> addUndo, Action<IUndoSnapshot<TId>> addRedo);
    void SaveUndoRedo(IEnumerable<IUndoSnapshot<TId>> undo, IEnumerable<IUndoSnapshot<TId>> redo);
    IUndoSnapshot<TId> CreateSnapshot(IEnumerable<TId> path, string changeId);
    void LoadChange(IUndoSnapshot<TId> snapshot, IChange change);
    void SaveChange(IUndoSnapshot<TId> snapshot, IChange change);
}
