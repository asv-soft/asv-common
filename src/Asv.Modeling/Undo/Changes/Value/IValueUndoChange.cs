namespace Asv.Modeling;

/// <summary>
/// Represents a typed undo change with old and new values.
/// </summary>
/// <typeparam name="T">The type of value affected by the change.</typeparam>
public interface IValueUndoChange<T> : IUndoChange
{
    /// <summary>
    /// Gets or sets the logical operation represented by this change.
    /// </summary>
    ChangeOperation Operation { get; set; }

    /// <summary>
    /// Gets or sets the value before the change was applied.
    /// </summary>
    T OldValue { get; set; }

    /// <summary>
    /// Gets or sets the value after the change was applied.
    /// </summary>
    T NewValue { get; set; }
}
