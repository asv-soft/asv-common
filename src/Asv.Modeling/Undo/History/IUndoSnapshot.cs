namespace Asv.Modeling;

/// <summary>
/// Describes one stored undo or redo history entry.
/// </summary>
public interface IUndoSnapshot
{
    /// <summary>
    /// Gets the navigation path to the node that owns the change handler.
    /// </summary>
    NavPath Path { get; }

    /// <summary>
    /// Gets the change registration identifier.
    /// </summary>
    string ChangeId { get; }

    /// <summary>
    /// Gets the identifier of the serialized change payload.
    /// </summary>
    Ulid DataRefId { get; }
}
