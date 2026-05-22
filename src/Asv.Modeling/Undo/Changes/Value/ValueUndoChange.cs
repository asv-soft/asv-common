using System.Buffers;
using System.Runtime.Serialization;
using MessagePack;

namespace Asv.Modeling;

/// <summary>
/// Represents a serializable undo change for a single value.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
[DataContract]
public struct ValueUndoChange<T> : IValueUndoChange<T>
{
    /// <inheritdoc />
    [DataMember(Order = 0)]
    public ChangeOperation Operation { get; set; }

    /// <inheritdoc />
    [DataMember(Order = 1)]
    public T OldValue { get; set; }

    /// <inheritdoc />
    [DataMember(Order = 2)]
    public T NewValue { get; set; }

    /// <inheritdoc />
    public void Serialize(IBufferWriter<byte> writer)
    {
        MessagePackSerializer.Serialize(writer, this);
    }

    /// <inheritdoc />
    public void Deserialize(ReadOnlySequence<byte> data)
    {
        this = MessagePackSerializer.Deserialize<ValueUndoChange<T>>(data);
    }
}
