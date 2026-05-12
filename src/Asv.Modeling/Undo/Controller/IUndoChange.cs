using System.Buffers;

namespace Asv.Modeling;

/// <summary>
/// Represents a serializable change that can be stored in undo history and later replayed.
/// </summary>
public interface IUndoChange
{
    /// <summary>
    /// Serializes this change into the specified binary writer.
    /// </summary>
    /// <param name="writer">The destination buffer writer.</param>
    void Serialize(IBufferWriter<byte> writer);

    /// <summary>
    /// Restores this change from serialized binary data.
    /// </summary>
    /// <param name="data">The serialized change payload.</param>
    void Deserialize(ReadOnlySequence<byte> data);
}

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

/// <summary>
/// Represents a typed undo change with old and new values.
/// </summary>
/// <typeparam name="T">The type of value affected by the change.</typeparam>
public interface IUndoChange<T> : IUndoChange
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
