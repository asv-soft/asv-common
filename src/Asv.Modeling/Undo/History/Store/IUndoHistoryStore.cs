namespace Asv.Modeling;

public interface IUndoHistoryStore : IDisposable
{
    void LoadUndoRedo(Action<IUndoSnapshot> addUndo, Action<IUndoSnapshot> addRedo);
    void SaveUndoRedo(IEnumerable<IUndoSnapshot> undo, IEnumerable<IUndoSnapshot> redo);
    IUndoSnapshot CreateSnapshot(NavPath path, string changeId);
    void LoadChange(IUndoSnapshot snapshot, IUndoChange undoChange);
    void SaveChange(IUndoSnapshot snapshot, IUndoChange undoChange);
}
