namespace Asv.Modeling;

/// <summary>
/// No-op undo history store.
/// </summary>
public class NullUndoHistoryStore : IUndoHistoryStore
{
    /// <summary>
    /// Gets the shared no-op undo history store instance.
    /// </summary>
    public static NullUndoHistoryStore Instance { get; } = new();

    /// <inheritdoc />
    public void Dispose()
    {
        // do nothing
    }

    /// <inheritdoc />
    public void LoadUndoRedo(Action<IUndoSnapshot> addUndo, Action<IUndoSnapshot> addRedo)
    {
        // do nothing
    }

    /// <inheritdoc />
    public void SaveUndoRedo(IEnumerable<IUndoSnapshot> undo, IEnumerable<IUndoSnapshot> redo)
    {
        // do nothing
    }

    /// <inheritdoc />
    public IUndoSnapshot CreateSnapshot(NavPath path, string changeId)
    {
        return new NullUndoSnapshot(path, changeId);
    }

    /// <inheritdoc />
    public void LoadChange(IUndoSnapshot snapshot, IUndoChange undoChange)
    {
        // do nothing
    }

    /// <inheritdoc />
    public void SaveChange(IUndoSnapshot snapshot, IUndoChange undoChange)
    {
        // do nothing
    }

    private class NullUndoSnapshot(NavPath path, string changeId) : IUndoSnapshot
    {
        /// <inheritdoc />
        public NavPath Path { get; } = path;

        /// <inheritdoc />
        public string ChangeId { get; } = changeId;

        /// <inheritdoc />
        public Ulid DataRefId { get; } = Ulid.NewUlid();
    }
}
