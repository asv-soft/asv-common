using System;

namespace Asv.IO;

public class ExampleMessage1: ExampleMessageBase
{
    public const string MessageName = "ExampleMessage1";
    public const int MessageId = 1;
    public static readonly Schema StaticSchema = new Schema.Builder()
        .Field(f => f
            .Name(nameof(Value1))
            .DataType(Int32Type.Default)
            .Title("Title  message  field 1")
            .Description("Description message field 1")
            .Read<ExampleMessage1>((m,v)=>Int32Type.Read(v,m._value1))
            
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
        .Build();
    

    private int _value1;
    private ushort _value2;
    
    

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

  

    public int Value1
    {
        get => _value1;
        set => _value1 = value;
    }

    public ushort Value2
    {
        get => _value2;
        set => _value2 = value;
    }

    public string? Value3 { get; set; }
    
    public override Schema Schema => StaticSchema;
    public override void Serialize(ISerializeVisitor visitor)
    {
        //Schema.Fields[0].Serialize(visitor, ref _value1);
    }

    public override void Deserialize(IDeserializeVisitor visitor)
    {
        throw new NotImplementedException();
    }


    public override string ToString()
    {
        return $"{Name}({Value1},{Value2},{Value3})";
    }
}

