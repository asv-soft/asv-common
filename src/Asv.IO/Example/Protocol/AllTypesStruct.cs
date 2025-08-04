using System;

namespace Asv.IO;

public record AllTypesStruct : IVisitable
{
    public static readonly Field BoolField = new Field.Builder()
        .Name(nameof(BoolValue))
        .DataType(BoolType.Default)
        .Title("Title  message  bool field")
        .Description("Description message bool field").Build();
    public bool BoolValue;
    
    public static readonly Field ByteField = new Field.Builder()
        .Name(nameof(ByteValue))
        .DataType(UInt8Type.Default)
        .Title("Title  message  byte field")
        .Description("Description message byte field").Build();
    public byte ByteValue;
    
    public static readonly Field SByteField = new Field.Builder()
        .Name(nameof(SByteValue))
        .DataType(Int8Type.Default)
        .Title("Title  message  sbyte field")
        .Description("Description message sbyte field").Build();
    public sbyte SByteValue;
    
    public static readonly Field UShortField = new Field.Builder()
        .Name(nameof(UShortValue))
        .DataType(UInt16Type.Default)
        .Title("Title  message  ushort field")
        .Description("Description message ushort field").Build();
    public short ShortValue;
    
    public static readonly Field ShortField = new Field.Builder()
        .Name(nameof(ShortValue))
        .DataType(Int16Type.Default)
        .Title("Title  message  short field")
        .Description("Description message short field").Build();
    public ushort UShortValue;
    
    public static readonly Field IntField = new Field.Builder()
        .Name(nameof(IntValue))
        .DataType(Int32Type.Default)
        .Title("Title  message  int field")
        .Description("Description message int field").Build();
    public int IntValue;
    
    public static readonly Field UIntField = new Field.Builder()
        .Name(nameof(UIntValue))
        .DataType(UInt32Type.Default)
        .Title("Title  message  uint field")
        .Description("Description message uint field").Build();
    public uint UIntValue;
    
    public static readonly Field LongField = new Field.Builder()
        .Name(nameof(LongValue))
        .DataType(Int64Type.Default)
        .Title("Title  message  long field")
        .Description("Description message long field").Build();
    public long LongValue;
    
    public static readonly Field ULongField = new Field.Builder()
        .Name(nameof(ULongValue))
        .DataType(UInt64Type.Default)
        .Title("Title  message  ulong field")
        .Description("Description message ulong field").Build();
    public ulong ULongValue;
    
    public static readonly Field FloatField = new Field.Builder()
        .Name(nameof(FloatValue))
        .DataType(FloatType.Default)
        .Title("Title  message  float field")
        .Description("Description message float field").Build();
    public float FloatValue;
    
    public static readonly Field DoubleField = new Field.Builder()
        .Name(nameof(DoubleValue))
        .DataType(DoubleType.Default)
        .Title("Title  message  double field")
        .Description("Description message double field").Build();
    public double DoubleValue;
    
    public static readonly Field StringField = new Field.Builder()
        .Name(nameof(StringValue))
        .DataType(StringType.Ascii)
        .Title("Title  message  string field")
        .Description("Description message string field").Build();
    
    public string StringValue = String.Empty;
    
    public static readonly Field DateTimeField = new Field.Builder()
        .Name(nameof(DateTimeValue))
        .DataType(DateTimeType.Default)
        .Title("Title  message  datetime field")
        .Description("Description message datetime field").Build();
    public DateTime DateTimeValue;
    
    public static readonly Field TimeSpanField = new Field.Builder()
        .Name(nameof(TimeSpanValue))
        .DataType(TimeSpanType.Default)
        .Title("Title  message  timespan field")
        .Description("Description message timespan field").Build();
    public TimeSpan TimeSpanValue;
    
    public static readonly Field TimeOnlyField = new Field.Builder()
        .Name(nameof(TimeOnlyValue))
        .DataType(TimeOnlyType.Default)
        .Title("Title  message  timeonly field")
        .Description("Description message timeonly field").Build();
    public TimeOnly TimeOnlyValue;
    
    public static readonly Field DateOnlyField = new Field.Builder()
        .Name(nameof(DateOnlyValue))
        .DataType(DateOnlyType.Default)
        .Title("Title  message  dateonly field")
        .Description("Description message dateonly field").Build();
    public DateOnly DateOnlyValue;
    
    public static readonly Field UInt8ArrayField = new Field.Builder()
        .Name(nameof(UInt8ArrayValue))
        .DataType(new ArrayType(UInt8Type.Default,10))
        .Title("Title  message  byte array field")
        .Description("Description message byte array field").Build();
    public byte[] UInt8ArrayValue = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
    
    public static readonly Field StructField = new Field.Builder()
        .Name(nameof(StructValue))
        .DataType(SubObject.StructType)
        .Title("Title  message  subobject field")
        .Description("Description message subobject field").Build();
    public SubObject StructValue = new();
    
    
    public static readonly Field BoolOptionalField = new Field.Builder()
        .Name(nameof(BoolOptionalValue))
        .DataType(BoolOptionalType.Default)
        .Title("Title  message  bool nullable field")
        .Description("Description message bool nullable field").Build();
    public bool? BoolOptionalValue;
    
    public static readonly Field UInt8OptionalField = new Field.Builder()
        .Name(nameof(UInt8OptionalValue))
        .DataType(UInt8OptionalType.Default)
        .Title("Title  message  byte nullable field")
        .Description("Description message byte nullable field").Build();
    public byte? UInt8OptionalValue;
    
    public static readonly Field Int8OptionalField = new Field.Builder()
        .Name(nameof(Int8OptionalValue))
        .DataType(Int8OptionalType.Default)
        .Title("Title  message  sbyte nullable field")
        .Description("Description message sbyte nullable field").Build();
    public sbyte? Int8OptionalValue;
    
    public static readonly Field Uint16OptionalField = new Field.Builder()
        .Name(nameof(Uint16OptionalValue))
        .DataType(UInt16OptionalType.Default)
        .Title("Title  message  ushort nullable field")
        .Description("Description message ushort nullable field").Build();
    public ushort? Uint16OptionalValue;
   
    public static readonly Field Int16OptionalField = new Field.Builder()
        .Name(nameof(Int16OptionalValue))
        .DataType(Int16OptionalType.Default)
        .Title("Title  message  short nullable field")
        .Description("Description message short nullable field").Build();
    public short? Int16OptionalValue;
    
    public static readonly Field Int32OptionalField = new Field.Builder()
        .Name(nameof(Int32OptionalValue))
        .DataType(Int32OptionalType.Default)
        .Title("Title  message  int nullable field")
        .Description("Description message int nullable field").Build();
    public int? Int32OptionalValue;
    
    public static readonly Field UInt32OptionalField = new Field.Builder()
        .Name(nameof(UInt32OptionalValue))
        .DataType(UInt32OptionalType.Default)
        .Title("Title  message  uint nullable field")
        .Description("Description message uint nullable field").Build();
    public uint? UInt32OptionalValue;
    
    public static readonly Field Int64OptionalField = new Field.Builder()
        .Name(nameof(Int64OptionalValue))
        .DataType(Int64OptionalType.Default)
        .Title("Title  message  long nullable field")
        .Description("Description message long nullable field").Build();
    public long? Int64OptionalValue;
    
    public static readonly Field UInt64OptionalField = new Field.Builder()
        .Name(nameof(UInt64OptionalValue))
        .DataType(UInt64OptionalType.Default)
        .Title("Title  message  ulong nullable field")
        .Description("Description message ulong nullable field").Build();
    public ulong? UInt64OptionalValue;
    
    public static readonly Field FloatOptionalField = new Field.Builder()
        .Name(nameof(FloatOptionalValue))
        .DataType(FloatOptionalType.Default)
        .Title("Title  message  float nullable field")
        .Description("Description message float nullable field").Build();
    public float? FloatOptionalValue;
    
    public static readonly Field DoubleOptionalField = new Field.Builder()
        .Name(nameof(DoubleOptionalValue))
        .DataType(DoubleOptionalType.Default)
        .Title("Title  message  double nullable field")
        .Description("Description message double nullable field").Build();
    public double? DoubleOptionalValue;
    
    public static readonly Field StringOptionalField = new Field.Builder()
        .Name(nameof(StringOptionalValue))
        .DataType(StringOptionalType.Ascii)
        .Title("Title  message  string nullable field")
        .Description("Description message string nullable field").Build();
    public string? StringOptionalValue;
    
    public static readonly Field DateTimeOptionalField = new Field.Builder()
        .Name(nameof(DateTimeOptionalValue))
        .DataType(DateTimeOptionalType.Default)
        .Title("Title  message  datetime nullable field")
        .Description("Description message datetime nullable field").Build();
    public DateTime? DateTimeOptionalValue;
    
    public static readonly Field TimeSpanOptionalField = new Field.Builder()
        .Name(nameof(TimeSpanOptionalValue))
        .DataType(TimeSpanOptionalType.Default)
        .Title("Title  message  timespan nullable field")
        .Description("Description message timespan nullable field").Build();
    public TimeSpan? TimeSpanOptionalValue;
    
    public static readonly Field TimeOnlyOptionalField = new Field.Builder()
        .Name(nameof(TimeOnlyOptionalValue))
        .DataType(TimeOnlyOptionalType.Default)
        .Title("Title  message  timeonly nullable field")
        .Description("Description message timeonly nullable field").Build();
    public TimeOnly? TimeOnlyOptionalValue;
    
    public static readonly Field DateOnlyOptionalField = new Field.Builder()
        .Name(nameof(DateOnlyOptionalValue))
        .DataType(DateOnlyOptionalType.Default)
        .Title("Title  message  dateonly nullable field")
        .Description("Description message dateonly nullable field").Build();
    public DateOnly? DateOnlyOptionalValue;
    
    public static readonly Field UInt8OptionalArrayField = new Field.Builder()
        .Name(nameof(UInt8ArrayValue))
        .DataType(new ArrayType(UInt8OptionalType.Default,5))
        .Title("Title  message  byte array field")
        .Description("Description message byte array field").Build();
    public readonly byte?[] UInt8OptionalArrayValue = [null, 1, 2, 3, null];

    
    public static readonly Field StructOptionalField = new Field.Builder()
        .Name(nameof(StructOptionalValue))
        .DataType(new OptionalStructType(SubObject.StructType))
        .Title("Title  message  struct field")
        .Description("Description message struct field").Build();
    public SubObject? StructOptionalValue;
    
    
    
    public void Accept(IVisitor visitor)
    {
        BoolType.Accept(visitor, BoolField,  ref BoolValue);
        UInt8Type.Accept(visitor, ByteField,  ref ByteValue);
        Int8Type.Accept(visitor, SByteField, ref SByteValue);
        UInt16Type.Accept(visitor, UShortField,  ref UShortValue);
        Int16Type.Accept(visitor, ShortField,  ref ShortValue);
        Int32Type.Accept(visitor, IntField,  ref IntValue);
        UInt32Type.Accept(visitor, UIntField,  ref UIntValue);
        Int64Type.Accept(visitor, LongField,  ref LongValue);
        UInt64Type.Accept(visitor, ULongField,  ref ULongValue);
        FloatType.Accept(visitor, FloatField,  ref FloatValue);
        DoubleType.Accept(visitor, DoubleField,  ref DoubleValue);
        StringType.Accept(visitor,StringField, ref StringValue);
        DateTimeType.Accept(visitor, DateTimeField, ref DateTimeValue);
        TimeSpanType.Accept(visitor, TimeSpanField, ref TimeSpanValue);
        TimeOnlyType.Accept(visitor, TimeOnlyField, ref TimeOnlyValue);
        DateOnlyType.Accept(visitor, DateOnlyField, ref DateOnlyValue);
        ArrayType.Accept(visitor, UInt8ArrayField, (index, v, f, t) => UInt8Type.Accept(v,f,t, ref UInt8ArrayValue[index]));
        StructType.Accept(visitor, StructField, StructValue);
        
        BoolOptionalType.Accept(visitor, BoolOptionalField, ref BoolOptionalValue);
        UInt8OptionalType.Accept(visitor, UInt8OptionalField,  ref UInt8OptionalValue);
        Int8OptionalType.Accept(visitor, Int8OptionalField, ref Int8OptionalValue);
        UInt16OptionalType.Accept(visitor, Uint16OptionalField,  ref Uint16OptionalValue);
        Int16OptionalType.Accept(visitor, Int16OptionalField, ref Int16OptionalValue);
        Int32OptionalType.Accept(visitor, Int32OptionalField, ref Int32OptionalValue);
        UInt32OptionalType.Accept(visitor, UInt32OptionalField, ref UInt32OptionalValue);
        Int64OptionalType.Accept(visitor, Int64OptionalField, ref Int64OptionalValue);
        UInt64OptionalType.Accept(visitor, UInt64OptionalField, ref UInt64OptionalValue);
        FloatOptionalType.Accept(visitor, FloatOptionalField, ref FloatOptionalValue);
        DoubleOptionalType.Accept(visitor, DoubleOptionalField, ref DoubleOptionalValue);
        StringOptionalType.Accept(visitor, StringOptionalField, ref StringOptionalValue);
        DateTimeOptionalType.Accept(visitor, DateTimeOptionalField, ref DateTimeOptionalValue);
        TimeSpanOptionalType.Accept(visitor, TimeSpanOptionalField, ref TimeSpanOptionalValue);
        TimeOnlyOptionalType.Accept(visitor, TimeOnlyOptionalField, ref TimeOnlyOptionalValue);
        DateOnlyOptionalType.Accept(visitor, DateOnlyOptionalField, ref DateOnlyOptionalValue);
        ArrayType.Accept(visitor, UInt8OptionalArrayField, (index, v, f, t) => UInt8OptionalType.Accept(v,f,t, ref UInt8OptionalArrayValue[index]));
        OptionalStructType.Accept(visitor, StructOptionalField, ref StructOptionalValue);

    }
}