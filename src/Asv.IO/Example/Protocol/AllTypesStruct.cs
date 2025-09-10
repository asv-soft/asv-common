using System;

namespace Asv.IO;

public record AllTypesStruct : IVisitable
{
    public static readonly Field BoolField = new Field.Builder()
        .Name(nameof(BoolValue))
        .DataType(BoolType.Default)
        .Title("Title  message  bool field")
        .Description("Description message bool field")
        .Build();
    private bool _boolValue;
    public bool BoolValue
    {
        get => _boolValue;
        set => _boolValue = value;
    }

    public static readonly Field ByteField = new Field.Builder()
        .Name(nameof(ByteValue))
        .DataType(UInt8Type.Default)
        .Title("Title  message  byte field")
        .Description("Description message byte field")
        .Build();
    private byte _byteValue;

    public byte ByteValue
    {
        get => _byteValue;
        set => _byteValue = value;
    }

    public static readonly Field SByteField = new Field.Builder()
        .Name(nameof(SByteValue))
        .DataType(Int8Type.Default)
        .Title("Title  message  sbyte field")
        .Description("Description message sbyte field")
        .Build();
    private sbyte _sByteValue;

    public sbyte SByteValue
    {
        get => _sByteValue;
        set => _sByteValue = value;
    }

    public static readonly Field UShortField = new Field.Builder()
        .Name(nameof(UShortValue))
        .DataType(UInt16Type.Default)
        .Title("Title  message  ushort field")
        .Description("Description message ushort field")
        .Build();

    private short _shortValue;

    public short ShortValue
    {
        get => _shortValue;
        set => _shortValue = value;
    }

    public static readonly Field ShortField = new Field.Builder()
        .Name(nameof(ShortValue))
        .DataType(Int16Type.Default)
        .Title("Title  message  short field")
        .Description("Description message short field")
        .Build();
    private ushort _uShortValue;

    public ushort UShortValue
    {
        get => _uShortValue;
        set => _uShortValue = value;
    }

    public static readonly Field IntField = new Field.Builder()
        .Name(nameof(IntValue))
        .DataType(Int32Type.Default)
        .Title("Title  message  int field")
        .Description("Description message int field")
        .Build();
    private int _intValue;

    public int IntValue
    {
        get => _intValue;
        set => _intValue = value;
    }

    public static readonly Field UIntField = new Field.Builder()
        .Name(nameof(UIntValue))
        .DataType(UInt32Type.Default)
        .Title("Title  message  uint field")
        .Description("Description message uint field")
        .Build();
    private uint _uIntValue;

    public uint UIntValue
    {
        get => _uIntValue;
        set => _uIntValue = value;
    }

    public static readonly Field LongField = new Field.Builder()
        .Name(nameof(LongValue))
        .DataType(Int64Type.Default)
        .Title("Title  message  long field")
        .Description("Description message long field")
        .Build();
    private long _longValue;

    public long LongValue
    {
        get => _longValue;
        set => _longValue = value;
    }

    public static readonly Field ULongField = new Field.Builder()
        .Name(nameof(ULongValue))
        .DataType(UInt64Type.Default)
        .Title("Title  message  ulong field")
        .Description("Description message ulong field")
        .Build();
    private ulong _uLongValue;

    public ulong ULongValue
    {
        get => _uLongValue;
        set => _uLongValue = value;
    }

    public static readonly Field FloatField = new Field.Builder()
        .Name(nameof(FloatValue))
        .DataType(FloatType.Default)
        .Title("Title  message  float field")
        .Description("Description message float field")
        .Build();
    private float _floatValue;

    public float FloatValue
    {
        get => _floatValue;
        set => _floatValue = value;
    }

    public static readonly Field DoubleField = new Field.Builder()
        .Name(nameof(DoubleValue))
        .DataType(DoubleType.Default)
        .Title("Title  message  double field")
        .Description("Description message double field")
        .Build();
    private double _doubleValue;

    public double DoubleValue
    {
        get => _doubleValue;
        set => _doubleValue = value;
    }

    public static readonly Field StringField = new Field.Builder()
        .Name(nameof(StringValue))
        .DataType(StringType.Ascii)
        .Title("Title  message  string field")
        .Description("Description message string field")
        .Build();
    private string _stringValue = string.Empty;

    public string StringValue
    {
        get => _stringValue;
        set => _stringValue = value;
    }

    public static readonly Field DateTimeField = new Field.Builder()
        .Name(nameof(DateTimeValue))
        .DataType(DateTimeType.Default)
        .Title("Title  message  datetime field")
        .Description("Description message datetime field")
        .Build();
    private DateTime _dateTimeValue;

    public DateTime DateTimeValue
    {
        get => _dateTimeValue;
        set => _dateTimeValue = value;
    }

    public static readonly Field TimeSpanField = new Field.Builder()
        .Name(nameof(TimeSpanValue))
        .DataType(TimeSpanType.Default)
        .Title("Title  message  timespan field")
        .Description("Description message timespan field")
        .Build();
    private TimeSpan _timeSpanValue;

    public TimeSpan TimeSpanValue
    {
        get => _timeSpanValue;
        set => _timeSpanValue = value;
    }

    public static readonly Field TimeOnlyField = new Field.Builder()
        .Name(nameof(TimeOnlyValue))
        .DataType(TimeOnlyType.Default)
        .Title("Title  message  timeonly field")
        .Description("Description message timeonly field")
        .Build();
    private TimeOnly _timeOnlyValue;

    public TimeOnly TimeOnlyValue
    {
        get => _timeOnlyValue;
        set => _timeOnlyValue = value;
    }

    public static readonly Field DateOnlyField = new Field.Builder()
        .Name(nameof(DateOnlyValue))
        .DataType(DateOnlyType.Default)
        .Title("Title  message  dateonly field")
        .Description("Description message dateonly field")
        .Build();
    private DateOnly _dateOnlyValue;

    public DateOnly DateOnlyValue
    {
        get => _dateOnlyValue;
        set => _dateOnlyValue = value;
    }

    public static readonly Field UInt8ArrayField = new Field.Builder()
        .Name(nameof(UInt8ArrayValue))
        .DataType(new ArrayType(UInt8Type.Default, 10))
        .Title("Title  message  byte array field")
        .Description("Description message byte array field")
        .Build();
    private byte[] _uInt8ArrayValue = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    public byte[] UInt8ArrayValue
    {
        get => _uInt8ArrayValue;
        set => _uInt8ArrayValue = value;
    }

    public static readonly Field StructField = new Field.Builder()
        .Name(nameof(StructValue))
        .DataType(SubObject.StructType)
        .Title("Title  message  subobject field")
        .Description("Description message subobject field")
        .Build();
    private SubObject _structValue = new();

    public SubObject StructValue
    {
        get => _structValue;
        set => _structValue = value;
    }

    public static readonly Field BoolOptionalField = new Field.Builder()
        .Name(nameof(BoolOptionalValue))
        .DataType(BoolOptionalType.Default)
        .Title("Title  message  bool nullable field")
        .Description("Description message bool nullable field")
        .Build();
    private bool? _boolOptionalValue;

    public bool? BoolOptionalValue
    {
        get => _boolOptionalValue;
        set => _boolOptionalValue = value;
    }

    public static readonly Field UInt8OptionalField = new Field.Builder()
        .Name(nameof(UInt8OptionalValue))
        .DataType(UInt8OptionalType.Default)
        .Title("Title  message  byte nullable field")
        .Description("Description message byte nullable field")
        .Build();
    private byte? _uInt8OptionalValue;

    public byte? UInt8OptionalValue
    {
        get => _uInt8OptionalValue;
        set => _uInt8OptionalValue = value;
    }

    public static readonly Field Int8OptionalField = new Field.Builder()
        .Name(nameof(Int8OptionalValue))
        .DataType(Int8OptionalType.Default)
        .Title("Title  message  sbyte nullable field")
        .Description("Description message sbyte nullable field")
        .Build();
    private sbyte? _int8OptionalValue;

    public sbyte? Int8OptionalValue
    {
        get => _int8OptionalValue;
        set => _int8OptionalValue = value;
    }

    public static readonly Field Uint16OptionalField = new Field.Builder()
        .Name(nameof(Uint16OptionalValue))
        .DataType(UInt16OptionalType.Default)
        .Title("Title  message  ushort nullable field")
        .Description("Description message ushort nullable field")
        .Build();
    private ushort? _uint16OptionalValue;

    public ushort? Uint16OptionalValue
    {
        get => _uint16OptionalValue;
        set => _uint16OptionalValue = value;
    }

    public static readonly Field Int16OptionalField = new Field.Builder()
        .Name(nameof(Int16OptionalValue))
        .DataType(Int16OptionalType.Default)
        .Title("Title  message  short nullable field")
        .Description("Description message short nullable field")
        .Build();
    private short? _int16OptionalValue;

    public short? Int16OptionalValue
    {
        get => _int16OptionalValue;
        set => _int16OptionalValue = value;
    }

    public static readonly Field Int32OptionalField = new Field.Builder()
        .Name(nameof(Int32OptionalValue))
        .DataType(Int32OptionalType.Default)
        .Title("Title  message  int nullable field")
        .Description("Description message int nullable field")
        .Build();
    private int? _int32OptionalValue;

    public int? Int32OptionalValue
    {
        get => _int32OptionalValue;
        set => _int32OptionalValue = value;
    }

    public static readonly Field UInt32OptionalField = new Field.Builder()
        .Name(nameof(UInt32OptionalValue))
        .DataType(UInt32OptionalType.Default)
        .Title("Title  message  uint nullable field")
        .Description("Description message uint nullable field")
        .Build();
    private uint? _uInt32OptionalValue;

    public uint? UInt32OptionalValue
    {
        get => _uInt32OptionalValue;
        set => _uInt32OptionalValue = value;
    }

    public static readonly Field Int64OptionalField = new Field.Builder()
        .Name(nameof(Int64OptionalValue))
        .DataType(Int64OptionalType.Default)
        .Title("Title  message  long nullable field")
        .Description("Description message long nullable field")
        .Build();
    private long? _int64OptionalValue;

    public long? Int64OptionalValue
    {
        get => _int64OptionalValue;
        set => _int64OptionalValue = value;
    }

    public static readonly Field UInt64OptionalField = new Field.Builder()
        .Name(nameof(UInt64OptionalValue))
        .DataType(UInt64OptionalType.Default)
        .Title("Title  message  ulong nullable field")
        .Description("Description message ulong nullable field")
        .Build();
    private ulong? _uInt64OptionalValue;

    public ulong? UInt64OptionalValue
    {
        get => _uInt64OptionalValue;
        set => _uInt64OptionalValue = value;
    }

    public static readonly Field FloatOptionalField = new Field.Builder()
        .Name(nameof(FloatOptionalValue))
        .DataType(FloatOptionalType.Default)
        .Title("Title  message  float nullable field")
        .Description("Description message float nullable field")
        .Build();
    private float? _floatOptionalValue;

    public float? FloatOptionalValue
    {
        get => _floatOptionalValue;
        set => _floatOptionalValue = value;
    }

    public static readonly Field DoubleOptionalField = new Field.Builder()
        .Name(nameof(DoubleOptionalValue))
        .DataType(DoubleOptionalType.Default)
        .Title("Title  message  double nullable field")
        .Description("Description message double nullable field")
        .Build();
    private double? _doubleOptionalValue;

    public double? DoubleOptionalValue
    {
        get => _doubleOptionalValue;
        set => _doubleOptionalValue = value;
    }

    public static readonly Field StringOptionalField = new Field.Builder()
        .Name(nameof(StringOptionalValue))
        .DataType(StringOptionalType.Ascii)
        .Title("Title  message  string nullable field")
        .Description("Description message string nullable field")
        .Build();
    private string? _stringOptionalValue;

    public string? StringOptionalValue
    {
        get => _stringOptionalValue;
        set => _stringOptionalValue = value;
    }

    public static readonly Field DateTimeOptionalField = new Field.Builder()
        .Name(nameof(DateTimeOptionalValue))
        .DataType(DateTimeOptionalType.Default)
        .Title("Title  message  datetime nullable field")
        .Description("Description message datetime nullable field")
        .Build();
    private DateTime? _dateTimeOptionalValue;

    public DateTime? DateTimeOptionalValue
    {
        get => _dateTimeOptionalValue;
        set => _dateTimeOptionalValue = value;
    }

    public static readonly Field TimeSpanOptionalField = new Field.Builder()
        .Name(nameof(TimeSpanOptionalValue))
        .DataType(TimeSpanOptionalType.Default)
        .Title("Title  message  timespan nullable field")
        .Description("Description message timespan nullable field")
        .Build();
    private TimeSpan? _timeSpanOptionalValue;

    public TimeSpan? TimeSpanOptionalValue
    {
        get => _timeSpanOptionalValue;
        set => _timeSpanOptionalValue = value;
    }

    public static readonly Field TimeOnlyOptionalField = new Field.Builder()
        .Name(nameof(TimeOnlyOptionalValue))
        .DataType(TimeOnlyOptionalType.Default)
        .Title("Title  message  timeonly nullable field")
        .Description("Description message timeonly nullable field")
        .Build();
    private TimeOnly? _timeOnlyOptionalValue;

    public TimeOnly? TimeOnlyOptionalValue
    {
        get => _timeOnlyOptionalValue;
        set => _timeOnlyOptionalValue = value;
    }

    public static readonly Field DateOnlyOptionalField = new Field.Builder()
        .Name(nameof(DateOnlyOptionalValue))
        .DataType(DateOnlyOptionalType.Default)
        .Title("Title  message  dateonly nullable field")
        .Description("Description message dateonly nullable field")
        .Build();
    private DateOnly? _dateOnlyOptionalValue;

    public DateOnly? DateOnlyOptionalValue
    {
        get => _dateOnlyOptionalValue;
        set => _dateOnlyOptionalValue = value;
    }

    public static readonly Field UInt8OptionalArrayField = new Field.Builder()
        .Name(nameof(UInt8ArrayValue))
        .DataType(new ArrayType(UInt8OptionalType.Default, 5))
        .Title("Title  message  byte array field")
        .Description("Description message byte array field")
        .Build();
    private byte?[] _uInt8OptionalArrayValue = [null, 1, 2, 3, null];

    public byte?[] UInt8OptionalArrayValue
    {
        get => _uInt8OptionalArrayValue;
        set => _uInt8OptionalArrayValue = value;
    }

    public static readonly Field StructOptionalField = new Field.Builder()
        .Name(nameof(StructOptionalValue))
        .DataType(new OptionalStructType(SubObject.StructType))
        .Title("Title  message  struct field")
        .Description("Description message struct field")
        .Build();
    private SubObject? _structOptionalValue;
    public SubObject? StructOptionalValue
    {
        get => _structOptionalValue;
        set => _structOptionalValue = value;
    }

    public void Accept(IVisitor visitor)
    {
        BoolType.Accept(visitor, BoolField, ref _boolValue);
        UInt8Type.Accept(visitor, ByteField, ref _byteValue);
        Int8Type.Accept(visitor, SByteField, ref _sByteValue);
        UInt16Type.Accept(visitor, UShortField, ref _uShortValue);
        Int16Type.Accept(visitor, ShortField, ref _shortValue);
        Int32Type.Accept(visitor, IntField, ref _intValue);
        UInt32Type.Accept(visitor, UIntField, ref _uIntValue);
        Int64Type.Accept(visitor, LongField, ref _longValue);
        UInt64Type.Accept(visitor, ULongField, ref _uLongValue);
        FloatType.Accept(visitor, FloatField, ref _floatValue);
        DoubleType.Accept(visitor, DoubleField, ref _doubleValue);
        StringType.Accept(visitor, StringField, ref _stringValue);
        DateTimeType.Accept(visitor, DateTimeField, ref _dateTimeValue);
        TimeSpanType.Accept(visitor, TimeSpanField, ref _timeSpanValue);
        TimeOnlyType.Accept(visitor, TimeOnlyField, ref _timeOnlyValue);
        DateOnlyType.Accept(visitor, DateOnlyField, ref _dateOnlyValue);
        ArrayType.Accept(
            visitor,
            UInt8ArrayField,
            (index, v, f, t) => UInt8Type.Accept(v, f, t, ref _uInt8ArrayValue[index])
        );
        StructType.Accept(visitor, StructField, _structValue);

        BoolOptionalType.Accept(visitor, BoolOptionalField, ref _boolOptionalValue);
        UInt8OptionalType.Accept(visitor, UInt8OptionalField, ref _uInt8OptionalValue);
        Int8OptionalType.Accept(visitor, Int8OptionalField, ref _int8OptionalValue);
        UInt16OptionalType.Accept(visitor, Uint16OptionalField, ref _uint16OptionalValue);
        Int16OptionalType.Accept(visitor, Int16OptionalField, ref _int16OptionalValue);
        Int32OptionalType.Accept(visitor, Int32OptionalField, ref _int32OptionalValue);
        UInt32OptionalType.Accept(visitor, UInt32OptionalField, ref _uInt32OptionalValue);
        Int64OptionalType.Accept(visitor, Int64OptionalField, ref _int64OptionalValue);
        UInt64OptionalType.Accept(visitor, UInt64OptionalField, ref _uInt64OptionalValue);
        FloatOptionalType.Accept(visitor, FloatOptionalField, ref _floatOptionalValue);
        DoubleOptionalType.Accept(visitor, DoubleOptionalField, ref _doubleOptionalValue);
        StringOptionalType.Accept(visitor, StringOptionalField, ref _stringOptionalValue);
        DateTimeOptionalType.Accept(visitor, DateTimeOptionalField, ref _dateTimeOptionalValue);
        TimeSpanOptionalType.Accept(visitor, TimeSpanOptionalField, ref _timeSpanOptionalValue);
        TimeOnlyOptionalType.Accept(visitor, TimeOnlyOptionalField, ref _timeOnlyOptionalValue);
        DateOnlyOptionalType.Accept(visitor, DateOnlyOptionalField, ref _dateOnlyOptionalValue);
        ArrayType.Accept(
            visitor,
            UInt8OptionalArrayField,
            (index, v, f, t) =>
                UInt8OptionalType.Accept(v, f, t, ref _uInt8OptionalArrayValue[index])
        );
        OptionalStructType.Accept(visitor, StructOptionalField, ref _structOptionalValue);
    }
}
