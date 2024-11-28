using System;

namespace Asv.IO;

public class ExampleMessage2 : ExampleMessageBase
{
    public const string MessageName = "ExampleMessage2";
    public const int MessageId = 2;

    
    public override string Name => MessageName;
    public override byte Id => MessageId;

    public int Value1 { get; set; }
    public ushort Value2 { get; set; }
    public string? Value3 { get; set; }
    public int Value4 { get; set; }
    public float Value5 { get; set; }
    public double Value6 { get; set; }
    public bool Value7 { get; set; }
    public long Value8 { get; set; }
    public ulong Value9 { get; set; }
    
    protected override void InternalDeserialize(ref ReadOnlySpan<byte> buffer)
    {
        Value1 = BinSerialize.ReadPackedInteger(ref buffer);
        Value2 = BinSerialize.ReadUShort(ref buffer);
        Value3 = BinSerialize.ReadString(ref buffer);
        Value4 = BinSerialize.ReadInt(ref buffer);
        Value5 = BinSerialize.ReadFloat(ref buffer);
        Value6 = BinSerialize.ReadDouble(ref buffer);
        Value7 = BinSerialize.ReadBool(ref buffer);
        Value8 = BinSerialize.ReadLong(ref buffer);
        Value9 = BinSerialize.ReadULong(ref buffer);
    }

    protected override void InternalSerialize(ref Span<byte> buffer)
    {
        BinSerialize.WritePackedInteger(ref buffer, Value1);
        BinSerialize.WriteUShort(ref buffer, Value2);
        BinSerialize.WriteString(ref buffer, Value3 ?? string.Empty);
        BinSerialize.WriteInt(ref buffer, Value4);
        BinSerialize.WriteFloat(ref buffer, Value5);
        BinSerialize.WriteDouble(ref buffer, Value6);
        BinSerialize.WriteBool(ref buffer, Value7);
        BinSerialize.WriteLong(ref buffer, Value8);
        BinSerialize.WriteULong(ref buffer, Value9);
    }

    protected override int InternalGetByteSize()
    {
        return BinSerialize.GetSizeForPackedInteger(Value1) +
               sizeof(ushort) +
               BinSerialize.GetSizeForString(Value3 ?? string.Empty) +
               sizeof(int) +
               sizeof(float) +
               sizeof(double) +
               sizeof(bool) +
               sizeof(long) +
               sizeof(ulong);
    }

    public override string ToString()
    {
        return $"{Name}({Value1},{Value2},{Value3},{Value4},{Value5},{Value6},{Value7},{Value8},{Value9})";
    }
}