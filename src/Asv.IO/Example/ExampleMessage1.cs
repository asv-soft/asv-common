using System;

namespace Asv.IO;

public class ExampleMessage1: ExampleMessageBase
{
    public const string MessageName = "ExampleMessage1";
    public const int MessageId = 1;

    protected override void InternalDeserialize(ref ReadOnlySpan<byte> buffer)
    {
        Value1 = BinSerialize.ReadPackedInteger(ref buffer);
        Value2 = BinSerialize.ReadUShort(ref buffer);
        Value3 = BinSerialize.ReadString(ref buffer);
    }

    protected override void InternalSerialize(ref Span<byte> buffer)
    {
        BinSerialize.WritePackedInteger(ref buffer, Value1);
        BinSerialize.WriteUShort(ref buffer, Value2);
        BinSerialize.WriteString(ref buffer, Value3 ?? string.Empty);
    }

    protected override int InternalGetByteSize()
    {
        return BinSerialize.GetSizeForPackedInteger(Value1) + sizeof(ushort) +
               BinSerialize.GetSizeForString(Value3 ?? string.Empty);
    }

    public override string Name => MessageName;
    public override byte Id => MessageId;

    public int Value1 { get; set; }
    public ushort Value2 { get; set; }
    public string? Value3 { get; set; }

    public override string ToString()
    {
        return $"{Name}({Value1},{Value2},{Value3})";
    }
}