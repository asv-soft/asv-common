using System.Buffers;
using MemoryPack;

namespace Asv.Modeling;

[MemoryPackable]
public partial struct KeyValueChange<TKey, TValue> : IChange
{
    public TKey Key { get; set; }
    public TValue OldValue { get; set; }
    public TValue NewValue { get; set; }

    public void Serialize(IBufferWriter<byte> writer)
    {
        MemoryPackSerializer.Serialize(writer, this);
    }

    public void Deserialize(ReadOnlySequence<byte> data)
    {
        MemoryPackSerializer.Deserialize(in data, ref this);
    }
}