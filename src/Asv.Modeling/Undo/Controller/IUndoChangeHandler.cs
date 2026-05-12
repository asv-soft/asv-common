namespace Asv.Modeling;

/// <summary>
/// Creates, undoes, and redoes changes registered in an undo controller.
/// </summary>
public interface IUndoChangeHandler
{
    /// <summary>
    /// Creates an empty change instance suitable for deserializing stored history data.
    /// </summary>
    /// <returns>A new change instance for this handler.</returns>
    IUndoChange Create();

    /// <summary>
    /// Applies the inverse of the specified change.
    /// </summary>
    /// <param name="undoChange">The change to undo.</param>
    /// <param name="cancel">A cancellation token for the undo operation.</param>
    /// <returns>A task-like value that completes when the undo operation finishes.</returns>
    ValueTask Undo(IUndoChange undoChange, CancellationToken cancel);

    /// <summary>
    /// Applies the specified change again after it has been undone.
    /// </summary>
    /// <param name="undoChange">The change to redo.</param>
    /// <param name="cancel">A cancellation token for the redo operation.</param>
    /// <returns>A task-like value that completes when the redo operation finishes.</returns>
    ValueTask Redo(IUndoChange undoChange, CancellationToken cancel);
}
