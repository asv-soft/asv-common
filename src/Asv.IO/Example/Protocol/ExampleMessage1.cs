using System;
using System.Collections.Generic;
using System.Linq;
using Asv.Common;

namespace Asv.IO;

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
        .DataType(new ArrayType(Int32Type.Default,1))
        .Title("Title  message  field 4")
        .Description("Description message field 4").Build();
    
    private readonly int[] _value4 = new int[1];
    
    public int[] Value4 => _value4;
    
    
    private static readonly Field Value5Field = new Field.Builder()
        .Name(nameof(Value5))
        .DataType(new ArrayType(StringType.Default,2))
        .Title("Title  message  field 5")
        .Description("Description message field 5").Build();
    private readonly string[] _value5 = new string[2];
    public string[] Value5 => _value5;
    
    private static readonly Field Value6Field = new Field.Builder()
        .Name(nameof(Value6))
        .DataType(new StructType(new SubObject().GetFields()))
        .Title("Title  message  field 6")
        .Description("Description message field 6").Build();
    private readonly SubObject[] _value6 =
    [
        new(),
        new()
    ];
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
        Int32Type.Accept(visitor, Value1Field, Value1Field.DataType, ref _value1);
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
        BinSerialize.ReadInt(ref buffer, ref _value1);
        BinSerialize.ReadUShort(ref buffer, ref _value2);
        _value3 = BinSerialize.ReadString(ref buffer);
        for (var i = 0; i < _value4.Length; i++)
        {
            BinSerialize.ReadInt(ref buffer, ref _value4[i]);
        }
        for (var i = 0; i < _value5.Length; i++)
        {
            _value5[i] = BinSerialize.ReadString(ref buffer);
        }
        foreach (var t in _value6)
        {
            t.Deserialize(ref buffer);
        }
        _value7.Deserialize(ref buffer);
        _value8.Resize((int)BinSerialize.ReadUInt(ref buffer));
        foreach (var t in _value8)
        {
            t.Deserialize(ref buffer);
        }
        _value9.Resize((int)BinSerialize.ReadUInt(ref buffer));
        for (var i = 0; i < _value9.Count; i++)
        {
            _value9[i] = BinSerialize.ReadInt(ref buffer);
        }
        
        
    }

    protected override void InternalSerialize(ref Span<byte> buffer)
    {
        BinSerialize.WriteInt(ref buffer, _value1);
        BinSerialize.WriteUShort(ref buffer, _value2);
        BinSerialize.WriteString(ref buffer, Value3 ?? string.Empty);
        foreach (var t in _value4)
        {
            BinSerialize.WriteInt(ref buffer, t);
        }
        foreach (var t in _value5)
        {
            BinSerialize.WriteString(ref buffer, t);
        }
        foreach (var t in _value6)
        {
            t.Serialize(ref buffer);
        }
        _value7.Serialize(ref buffer);
        BinSerialize.WriteUInt(ref buffer, (uint)_value8.Count);
        foreach (var t in _value8)
        {
            t.Serialize(ref buffer);
        }
        BinSerialize.WriteUInt(ref buffer, (uint)_value9.Count);
        foreach (var t in _value9)
        {
            BinSerialize.WriteInt(ref buffer, t);
        }
    }

    protected override int InternalGetByteSize()
    {
        return sizeof(int) // value1 as int
            + sizeof(ushort)
            + BinSerialize.GetSizeForString(_value3)
            + _value4.Length * sizeof(int)
            + _value5.Sum(BinSerialize.GetSizeForString)
            + _value6.Sum(x=>x.GetByteSize())
            + _value7.GetByteSize()
            + sizeof(uint) // size of list
            + _value8.Sum(x=>x.GetByteSize())
            + sizeof(uint) // size of list
            + _value9.Count * sizeof(int);
            
            
    }

    public override string Name => MessageName;
    public override byte Id => MessageId;

    public override string ToString()
    {
        return $"{Name}({this.PrintValues()})";
    }
}

