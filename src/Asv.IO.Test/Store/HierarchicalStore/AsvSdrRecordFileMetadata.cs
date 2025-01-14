using System;
using System.Collections.Generic;
using System.Linq;

namespace Asv.IO.Test.HierarchicalStore;

public class AsvSdrRecordFileMetadata:ISizedSpanSerializable
{
    public ExampleMessage2 Info { get; } = new();

    public IList<ExampleMessage1> Tags { get; } = new List<ExampleMessage1>();

    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        Info.Deserialize(ref buffer);
        var count = BinSerialize.ReadUShort(ref buffer);
        Tags.Clear();
        for (var i = 0; i < count; i++)
        {
            var tag = new ExampleMessage1();
            tag.Deserialize(ref buffer);
            Tags.Add(tag);
        }
    }

    public void Serialize(ref Span<byte> buffer)
    {
        Info.Serialize(ref buffer);
        BinSerialize.WriteUShort(ref buffer,(ushort)Tags.Count);
        foreach (var tag in Tags)
        {
            tag.Serialize(ref buffer);
        }
    }

    public int GetByteSize()
    {
        var size = Info.GetByteSize()  + sizeof(ushort);
        if (Tags != null)
        {
            size += Tags.Sum(x => x.GetByteSize());
        }
        return size;
    }
}