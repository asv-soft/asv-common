using System.Buffers;
using MemoryPack;

namespace Asv.Modeling;

[MemoryPackable]
public partial struct ObservableCollectionChangeEvent<T> : IChange<T>
{
    public ChangeOperation Operation { get; set; }
    public int OldIndex { get; set; }
    public int NewIndex { get; set; }
    public T OldValue { get; set; }
    public T NewValue { get; set; }

    public void Serialize(IBufferWriter<byte> writer)
    {
        MemoryPackSerializer.Serialize(writer, this);
    }

    public void Deserialize(ReadOnlySequence<byte> data)
    {
        MemoryPackSerializer.Deserialize(in data, ref this);
    }
}
