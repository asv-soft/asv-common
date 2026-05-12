using System.Buffers;
using System.Runtime.Serialization;
using MessagePack;

namespace Asv.Modeling;

[DataContract]
public struct KeyValueUndoChange<TKey, TValue> : IValueUndoChange<TValue>
{
    [DataMember(Order = 0)]
    public ChangeOperation Operation { get; set; }

    [DataMember(Order = 1)]
    public TKey Key { get; set; }

    [DataMember(Order = 2)]
    public TValue OldValue { get; set; }

    [DataMember(Order = 3)]
    public TValue NewValue { get; set; }

    public void Serialize(IBufferWriter<byte> writer)
    {
        MessagePackSerializer.Serialize(writer, this);
    }

    public void Deserialize(ReadOnlySequence<byte> data)
    {
        this = MessagePackSerializer.Deserialize<KeyValueUndoChange<TKey, TValue>>(data);
    }
}
