using System;
using System.Collections.Generic;

namespace Asv.IO;

public record SubObject : IVisitable
{
    private static readonly Field Field1Field = new Field.Builder()
        .Name(nameof(Field1))
        .DataType(Int8Type.Default)
        .Title("Title  message  field 1")
        .Description("Description message field 1").Build();
    
    private sbyte _field1;
    public sbyte Field1
    {
        get => _field1;
        set => _field1 = value;
    }

    private static readonly Field Field2Field = new Field.Builder()
        .Name(nameof(Field2))
        .DataType(UInt8Type.Default)
        .Title("Title  message  field 2")
        .Description("Description message field 2").Build();
    private byte _field2;
    public byte Field2
    {
        get => _field2;
        set => _field2 = value;
    }

    private static readonly Field Field3Field = new Field.Builder()
        .Name(nameof(Field3))
        .DataType(Int16Type.Default)
        .Title("Title  message  field 3")
        .Description("Description message field 3").Build();
    private short _field3;
    public short Field3
    {
        get => _field3;
        set => _field3 = value;
    }

    private static readonly Field Field4Field = new Field.Builder()
        .Name(nameof(Field4))
        .DataType(UInt16Type.Default)
        .Title("Title  message  field 4")
        .Description("Description message field 4").Build();
    private ushort _field4;
    public ushort Field4
    {
        get => _field4;
        set => _field4 = value;
    }

    public void Accept(IVisitor visitor)
    {
        Int8Type.Accept(visitor,Field1Field, ref _field1);
        UInt8Type.Accept(visitor,Field2Field, ref _field2);
        Int16Type.Accept(visitor,Field3Field, ref _field3);
        UInt16Type.Accept(visitor,Field4Field, ref _field4);
    }
}

public class ExampleMessage1: ExampleMessageBase
{
    public const string MessageName = "ExampleMessage1";
    public const int MessageId = 1;

    private static readonly Field Value1Field = new Field.Builder()
        .Name(nameof(Value1))
        .DataType(Int32Type.Default)
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
    
    private readonly int[] _value4 = new int[1];
    
    public int[] Value4 => _value4;
    
    
    private static readonly Field Value5Field = new Field.Builder()
        .Name(nameof(Value5))
        .DataType(StringType.Default)
        .Title("Title  message  field 5")
        .Description("Description message field 5").Build();
    private readonly string[] _value5 = new string[2];
    public string[] Value5 => _value5;
    
    private static readonly Field Value6Field = new Field.Builder()
        .Name(nameof(Value6))
        .DataType(new StructType(new SubObject().GetFields()))
        .Title("Title  message  field 6")
        .Description("Description message field 6").Build();
    private readonly SubObject[] _value6 = new SubObject[2];
    public SubObject[] Value6 => _value6;
    

    private static readonly Field Value7Field = new Field.Builder()
        .Name(nameof(Value7))
        .DataType(new StructType(new SubObject().GetFields()))
        .Title("Title  message  field 7")
        .Description("Description message field 7").Build();
    private readonly SubObject _value7 = new();
    public SubObject Value7 => _value7;
    
    private static readonly Field Value8Field = new Field.Builder()
        .Name(nameof(Value8))
        .DataType(new ListType(new StructType(new SubObject().GetFields())))
        .Title("Title  message  field 8")
        .Description("Description message field 8").Build();
    private readonly List<SubObject> _value8 = new();
    public IList<SubObject> Value8 => _value8;
    private static readonly Field Value9Field = new Field.Builder()
        .Name(nameof(Value9))
        .DataType(new ListType(Int32Type.Default))
        .Title("Title  message  field 9")
        .Description("Description message field 9").Build();
    
    private readonly List<int> _value9 = new();
    public IList<int> Value9 => _value9;

    public override void Accept(IVisitor visitor)
    {
        Int32Type.Accept(visitor, Value1Field, ref _value1);
        UInt16Type.Accept(visitor, Value2Field, ref _value2);
        StringType.Accept(visitor, Value3Field, ref _value3);
        ArrayType.Accept(visitor, Value4Field, _value4.Length, (index,v) =>
        {
            Int32Type.Accept(v,Value4Field, ref _value4[index]);
        });
        ArrayType.Accept(visitor, Value5Field, _value5.Length, (index,v) =>
        {
            StringType.Accept(v,Value5Field, ref _value5[index]);
        });
        ArrayType.Accept(visitor, Value6Field, _value6.Length, (index,v) =>
        {
            StructType.Accept(v, Value6Field, _value6[index]);
        });
        StructType.Accept(visitor, Value7Field, _value7);
        ListType.Accept(visitor, Value8Field, _value8, (index, v) =>
        {
            StructType.Accept(v, Value8Field, _value8[index]);
        });
        ListType.Accept(visitor, Value9Field, _value9, (index, v) =>
        {
            var temp = _value9[index];
            Int32Type.Accept(v,Value9Field, ref temp);
            _value9[index] = temp;
        });
    }

    protected override void InternalDeserialize(ref ReadOnlySpan<byte> buffer)
    {
        BinSerialize.ReadPackedInteger(ref buffer, ref _value1);
        BinSerialize.ReadUShort(ref buffer, ref _value2);
        Value3 = BinSerialize.ReadString(ref buffer);
    }

    protected override void InternalSerialize(ref Span<byte> buffer)
    {
        BinSerialize.WritePackedInteger(ref buffer, _value1);
        BinSerialize.WriteUShort(ref buffer, _value2);
        BinSerialize.WriteString(ref buffer, Value3 ?? string.Empty);
    }

    protected override int InternalGetByteSize()
    {
        return BinSerialize.GetSizeForPackedInteger(Value1) 
               + sizeof(ushort) 
               + BinSerialize.GetSizeForString(Value3 ?? string.Empty);
    }

    public override string Name => MessageName;
    public override byte Id => MessageId;

    public override string ToString()
    {
        return $"{Name}({Value1},{Value2},{Value3})";
    }
}

