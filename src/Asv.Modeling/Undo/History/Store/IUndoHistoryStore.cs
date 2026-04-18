namespace Asv.Modeling;

public interface IUndoHistoryStore : IDisposable
{
    void LoadUndoRedo(Action<IUndoSnapshot> addUndo, Action<IUndoSnapshot> addRedo);
    void SaveUndoRedo(IEnumerable<IUndoSnapshot> undo, IEnumerable<IUndoSnapshot> redo);
    IUndoSnapshot CreateSnapshot(NavPath path, string changeId);
    void LoadChange(IUndoSnapshot snapshot, IChange change);
    void SaveChange(IUndoSnapshot snapshot, IChange change);
}
