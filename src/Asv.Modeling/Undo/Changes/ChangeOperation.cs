namespace Asv.Modeling;

/// <summary>
/// Describes the logical operation represented by an undo change.
/// </summary>
public enum ChangeOperation : byte
{
    /// <summary>
    /// Existing data was modified.
    /// </summary>
    Update = 0,

    /// <summary>
    /// New data was added.
    /// </summary>
    Create = 1,

    /// <summary>
    /// Data was read without changing state.
    /// </summary>
    Read = 2,

    /// <summary>
    /// Existing data was removed.
    /// </summary>
    Delete = 3,
}
