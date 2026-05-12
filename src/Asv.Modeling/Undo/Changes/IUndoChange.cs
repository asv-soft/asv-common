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
