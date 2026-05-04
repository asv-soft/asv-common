namespace Asv.Modeling;

public class NullUndoHistoryStore : IUndoHistoryStore
{
    public static NullUndoHistoryStore Instance { get; } = new();
    
    public void Dispose()
    {
        // do nothing
    }

    public void LoadUndoRedo(Action<IUndoSnapshot> addUndo, Action<IUndoSnapshot> addRedo)
    {
        // do nothing
    }

    public void SaveUndoRedo(IEnumerable<IUndoSnapshot> undo, IEnumerable<IUndoSnapshot> redo)
    {
        // do nothing
    }

    public IUndoSnapshot CreateSnapshot(NavPath path, string changeId)
    {
        return new NullUndoSnapshot(path, changeId);
    }

    public void LoadChange(IUndoSnapshot snapshot, IChange change)
    {
        // do nothing
    }

    public void SaveChange(IUndoSnapshot snapshot, IChange change)
    {
        // do nothing
    }

    private class NullUndoSnapshot(NavPath path, string changeId) : IUndoSnapshot
    {
        public NavPath Path { get; } = path;
        public string ChangeId { get; } = changeId;
        public Ulid DataRefId { get; } = Ulid.NewUlid();
    }
}

