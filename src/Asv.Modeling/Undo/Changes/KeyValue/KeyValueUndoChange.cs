using System.Buffers;
using System.Runtime.Serialization;
using MessagePack;

namespace Asv.Modeling;

/// <summary>
/// Represents a serializable undo change for a keyed value.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
[DataContract]
public struct KeyValueUndoChange<TKey, TValue> : IValueUndoChange<TValue>
{
    /// <inheritdoc />
    [DataMember(Order = 0)]
    public ChangeOperation Operation { get; set; }

    /// <summary>
    /// Gets or sets the key of the changed value.
    /// </summary>
    [DataMember(Order = 1)]
    public TKey Key { get; set; }

    /// <inheritdoc />
    [DataMember(Order = 2)]
    public TValue OldValue { get; set; }

    /// <inheritdoc />
    [DataMember(Order = 3)]
    public TValue NewValue { get; set; }

    /// <inheritdoc />
    public void Serialize(IBufferWriter<byte> writer)
    {
        MessagePackSerializer.Serialize(writer, this);
    }

    /// <inheritdoc />
    public void Deserialize(ReadOnlySequence<byte> data)
    {
        this = MessagePackSerializer.Deserialize<KeyValueUndoChange<TKey, TValue>>(data);
    }
}
