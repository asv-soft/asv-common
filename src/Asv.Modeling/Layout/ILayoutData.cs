using System.Buffers;

namespace Asv.Modeling;

/// <summary>
/// Represents serializable display state that can be persisted by the layout store.
/// </summary>
public interface ILayoutData
{
    /// <summary>
    /// Serializes this display state into the specified binary writer.
    /// </summary>
    /// <param name="writer">The destination buffer writer.</param>
    void Serialize(IBufferWriter<byte> writer);

    /// <summary>
    /// Restores this display state from serialized binary data.
    /// </summary>
    /// <param name="data">The serialized layout payload.</param>
    void Deserialize(ReadOnlySequence<byte> data);
}
