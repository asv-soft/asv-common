using System.Buffers;
using MemoryPack;
using MessagePack;

namespace Asv.Modeling;

[MemoryPackable]
public partial struct ScalarChange<T> : IChange
{
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
