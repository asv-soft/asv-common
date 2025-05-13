using System;

namespace Asv.IO;

public class ExampleMessage2 : ExampleMessageBase
{
    public const string MessageName = "ExampleMessage2";
    public const int MessageId = 2;
    public static readonly Schema StaticSchema = new Schema.Builder()
        .Field(f => f
            .Name(nameof(Value1))
            .DataType(Int32Type.Default)
            .Title("Title  message  field 1")
            .Description("Description message field 1")
        )
        .Field(f => f
            .Name(nameof(Value2))
            .DataType(UInt16Type.Default)
            .Title("Title  message  field 2")
            .Description("Description message field 2")
        )
        .Field(f => f
            .Name(nameof(Value3))
            .DataType(StringType.Default)
            .Title("Title  message  field 3")
            .Description("Description message field 3")
        )
        .Field(f => f
            .Name(nameof(Value4))
            .DataType(Int32Type.Default)
            .Title("Title  message  field 4")
            .Description("Description message field 4")
        )
        .Field(f => f
            .Name(nameof(Value5))
            .DataType(FloatType.Default)
            .Title("Title  message  field 5")
            .Description("Description message field 5")
        )
        .Field(f => f
            .Name(nameof(Value6))
            .DataType(DoubleType.Default)
            .Title("Title  message  field 6")
            .Description("Description message field 6")
        )
        .Field(f => f
            .Name(nameof(Value7))
            .DataType(BooleanType.Default)
            .Title("Title  message  field 7")
            .Description("Description message field 7")
        )
        .Field(f => f
            .Name(nameof(Value8))
            .DataType(Int64Type.Default)
            .Title("Title  message  field 8")
            .Description("Description message field 8")
        )
        .Field(f => f
            .Name(nameof(Value9))
            .DataType(UInt64Type.Default)
            .Title("Title  message  field 9")
            .Description("Description message field 9")
        )
        .Build();
    
    public override string Name => MessageName;
    public override byte Id => MessageId;
    public override Schema Schema => StaticSchema;
    public override void Serialize(ISerializeVisitor visitor)
    {
        throw new NotImplementedException();
    }

    public override void Deserialize(IDeserializeVisitor visitor)
    {
        throw new NotImplementedException();
    }

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