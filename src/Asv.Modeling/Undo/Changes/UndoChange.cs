using System.Buffers;
using System.Runtime.Serialization;
using MessagePack;

namespace Asv.Modeling;

[DataContract]
public struct UndoChange<T> : IUndoChange<T>
{
    [DataMember(Order = 0)]
    public ChangeOperation Operation { get; set; }

    [DataMember(Order = 1)]
    public T OldValue { get; set; }

    [DataMember(Order = 2)]
    public T NewValue { get; set; }

    public void Serialize(IBufferWriter<byte> writer)
    {
        MessagePackSerializer.Serialize(writer, this);
    }

    public void Deserialize(ReadOnlySequence<byte> data)
    {
        this = MessagePackSerializer.Deserialize<UndoChange<T>>(data);
    }
}
