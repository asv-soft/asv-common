namespace Asv.Modeling;

/// <summary>
/// Stores undo and redo stacks together with serialized change payloads.
/// </summary>
public interface IUndoHistoryStore : IDisposable
{
    /// <summary>
    /// Loads persisted undo and redo stack snapshots.
    /// </summary>
    /// <param name="addUndo">Callback used to add loaded undo snapshots.</param>
    /// <param name="addRedo">Callback used to add loaded redo snapshots.</param>
    void LoadUndoRedo(Action<IUndoSnapshot> addUndo, Action<IUndoSnapshot> addRedo);

    /// <summary>
    /// Saves undo and redo stack snapshots.
    /// </summary>
    /// <param name="undo">The undo stack snapshots to save.</param>
    /// <param name="redo">The redo stack snapshots to save.</param>
    void SaveUndoRedo(IEnumerable<IUndoSnapshot> undo, IEnumerable<IUndoSnapshot> redo);

    /// <summary>
    /// Creates a new snapshot for a change.
    /// </summary>
    /// <param name="path">The path to the node that owns the change handler.</param>
    /// <param name="changeId">The change registration identifier.</param>
    /// <returns>A new undo snapshot.</returns>
    IUndoSnapshot CreateSnapshot(NavPath path, string changeId);

    /// <summary>
    /// Loads a serialized change payload into an undo change instance.
    /// </summary>
    /// <param name="snapshot">The snapshot that references the serialized payload.</param>
    /// <param name="undoChange">The change instance to deserialize into.</param>
    void LoadChange(IUndoSnapshot snapshot, IUndoChange undoChange);

    /// <summary>
    /// Saves a serialized change payload referenced by the specified snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot that references the serialized payload.</param>
    /// <param name="undoChange">The change payload to serialize.</param>
    void SaveChange(IUndoSnapshot snapshot, IUndoChange undoChange);
}
