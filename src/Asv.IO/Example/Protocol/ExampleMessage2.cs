using System;

namespace Asv.IO;

public class ExampleMessage2 : ExampleMessageBase
{
    public const string MessageName = "ExampleMessage2";
    public const int MessageId = 2;
    
    public override string Name => MessageName;
    public override byte Id => MessageId;

    public override void Accept(IVisitor visitor)
    {
        Int32Type.Accept(visitor, Value1Field, ref _value1);
        UInt16Type.Accept(visitor, Value2Field, ref _value2);
        StringType.Accept(visitor, Value3Field, ref _value3);
        Int32Type.Accept(visitor, Value4Field, ref _value4);
        FloatType.Accept(visitor, Value5Field, ref _value5);
        DoubleType.Accept(visitor, Value6Field, ref _value6);
        BooleanType.Visit(visitor, Value7Field, ref _value7);
        Int64Type.Accept(visitor, Value8Field, ref _value8);
        UInt64Type.Accept(visitor, Value9Field, ref _value9);
    }




    private static readonly Field Value1Field = new Field.Builder()
        .Name(nameof(Value1))
        .DataType(UInt32Type.Default)
        .Title("Title  message  field 1")
        .Description("Description message field 1").Build();
    private int _value1;
    public int Value1
    {
        get => _value1;
        set => _value1 = value;
    }

    private static readonly Field Value2Field = new Field.Builder()
        .Name(nameof(Value2))
        .DataType(UInt16Type.Default)
        .Title("Title  message  field 2")
        .Description("Description message field 2").Build();
    private ushort _value2;
    public ushort Value2
    {
        get => _value2;
        set => _value2 = value;
    }

    private static readonly Field Value3Field = new Field.Builder()
        .Name(nameof(Value3))
        .DataType(StringType.Default)
        .Title("Title  message  field 3")
        .Description("Description message field 3").Build();
    private string _value3 = string.Empty;
    
    public string Value3
    {
        get => _value3;
        set => _value3 = value;
    }

    private static readonly Field Value4Field = new Field.Builder()
        .Name(nameof(Value4))
        .DataType(Int32Type.Default)
        .Title("Title  message  field 4")
        .Description("Description message field 4").Build();
    private int _value4;
    
    public int Value4
    {
        get => _value4;
        set => _value4 = value;
    }

    private static readonly Field Value5Field = new Field.Builder()
        .Name(nameof(Value5))
        .DataType(FloatType.Default)
        .Title("Title  message  field 5")
        .Description("Description message field 5").Build();
    private float _value5;
    
    public float Value5
    {
        get => _value5;
        set => _value5 = value;
    }

    private static readonly Field Value6Field = new Field.Builder()
        .Name(nameof(Value6))
        .DataType(DoubleType.Default)
        .Title("Title  message  field 6")
        .Description("Description message field 6").Build();
    private double _value6;
    
    public double Value6
    {
        get => _value6;
        set => _value6 = value;
    }

    private static readonly Field Value7Field = new Field.Builder()
        .Name(nameof(Value7))
        .DataType(BooleanType.Default)
        .Title("Title  message  field 7")
        .Description("Description message field 7").Build();
    private bool _value7;
   
    public bool Value7
    {
        get => _value7;
        set => _value7 = value;
    }

    private static readonly Field Value8Field = new Field.Builder()
        .Name(nameof(Value8))
        .DataType(Int64Type.Default)
        .Title("Title  message  field 8")
        .Description("Description message field 8").Build();
    private long _value8;
    
    public long Value8
    {
        get => _value8;
        set => _value8 = value;
    }

    
    private static readonly Field Value9Field = new Field.Builder()
        .Name(nameof(Value9))
        .DataType(UInt64Type.Default)
        .Title("Title  message  field 9")
        .Description("Description message field 9").Build();
    private ulong _value9;
    public ulong Value9
    {
        get => _value9;
        set => _value9 = value;
    }

    protected override void InternalDeserialize(ref ReadOnlySpan<byte> buffer)
    {
        BinSerialize.ReadPackedInteger(ref buffer, ref _value1);
        BinSerialize.ReadUShort(ref buffer, ref _value2);
        _value3 = BinSerialize.ReadString(ref buffer);
        BinSerialize.ReadInt(ref buffer, ref _value4);
        BinSerialize.ReadFloat(ref buffer, ref _value5);
        BinSerialize.ReadDouble(ref buffer, ref _value6);
        BinSerialize.ReadBool(ref buffer, ref _value7);
        BinSerialize.ReadLong(ref buffer, ref _value8);
        BinSerialize.ReadULong(ref buffer, ref _value9);
    }

    protected override void InternalSerialize(ref Span<byte> buffer)
    {
        BinSerialize.WritePackedInteger(ref buffer, _value1);
        BinSerialize.WriteUShort(ref buffer, _value2);
        BinSerialize.WriteString(ref buffer, _value3 ?? string.Empty);
        BinSerialize.WriteInt(ref buffer, _value4);
        BinSerialize.WriteFloat(ref buffer, _value5);
        BinSerialize.WriteDouble(ref buffer, _value6);
        BinSerialize.WriteBool(ref buffer, _value7);
        BinSerialize.WriteLong(ref buffer, _value8);
        BinSerialize.WriteULong(ref buffer, _value9);
    }

    protected override int InternalGetByteSize()
    {
        return BinSerialize.GetSizeForPackedInteger(Value1) + // value1 is int
               sizeof(ushort) + // value2 is ushort
               BinSerialize.GetSizeForString(Value3 ?? string.Empty) + // value3 is string
               sizeof(int) + // value4 is int
               sizeof(float) + // value5 is float
               sizeof(double) + // value6 is double
               sizeof(bool) + // value7 is bool
               sizeof(long) + // value8 is long
               sizeof(ulong); // value9 is ulong
    }

    public override string ToString()
    {
        return $"{Name}({Value1},{Value2},{Value3},{Value4},{Value5},{Value6},{Value7},{Value8},{Value9})";
    }
}