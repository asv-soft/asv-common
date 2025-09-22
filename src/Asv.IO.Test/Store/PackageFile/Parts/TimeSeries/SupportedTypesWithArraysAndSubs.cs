namespace Asv.IO.Test
{
    public record BoolSub : IVisitable
    {
        public static readonly Field Field = new Field.Builder()
            .Name(nameof(Value))
            .DataType(BoolType.Default)
            .Build();
        private bool _v;
        public bool Value
        {
            get => _v;
            set => _v = value;
        }

        public void Accept(IVisitor visitor) => BoolType.Accept(visitor, Field, ref _v);
    }

    public record CharSub : IVisitable
    {
        public static readonly Field Field = new Field.Builder()
            .Name(nameof(Value))
            .DataType(CharType.Ascii)
            .Build();
        private char _v;
        public char Value
        {
            get => _v;
            set => _v = value;
        }

        public void Accept(IVisitor visitor) => CharType.Accept(visitor, Field, ref _v);
    }

    public record Int8Sub : IVisitable
    {
        public static readonly Field Field = new Field.Builder()
            .Name(nameof(Value))
            .DataType(Int8Type.Default)
            .Build();
        private sbyte _v;
        public sbyte Value
        {
            get => _v;
            set => _v = value;
        }

        public void Accept(IVisitor visitor) => Int8Type.Accept(visitor, Field, ref _v);
    }

    public record Int16Sub : IVisitable
    {
        public static readonly Field Field = new Field.Builder()
            .Name(nameof(Value))
            .DataType(Int16Type.Default)
            .Build();
        private short _v;
        public short Value
        {
            get => _v;
            set => _v = value;
        }

        public void Accept(IVisitor visitor) => Int16Type.Accept(visitor, Field, ref _v);
    }

    public record Int32Sub : IVisitable
    {
        public static readonly Field Field = new Field.Builder()
            .Name(nameof(Value))
            .DataType(Int32Type.Default)
            .Build();
        private int _v;
        public int Value
        {
            get => _v;
            set => _v = value;
        }

        public void Accept(IVisitor visitor) => Int32Type.Accept(visitor, Field, ref _v);
    }

    public record Int64Sub : IVisitable
    {
        public static readonly Field Field = new Field.Builder()
            .Name(nameof(Value))
            .DataType(Int64Type.Default)
            .Build();
        private long _v;
        public long Value
        {
            get => _v;
            set => _v = value;
        }

        public void Accept(IVisitor visitor) => Int64Type.Accept(visitor, Field, ref _v);
    }

    public record UInt8Sub : IVisitable
    {
        public static readonly Field Field = new Field.Builder()
            .Name(nameof(Value))
            .DataType(UInt8Type.Default)
            .Build();
        private byte _v;
        public byte Value
        {
            get => _v;
            set => _v = value;
        }

        public void Accept(IVisitor visitor) => UInt8Type.Accept(visitor, Field, ref _v);
    }

    public record UInt16Sub : IVisitable
    {
        public static readonly Field Field = new Field.Builder()
            .Name(nameof(Value))
            .DataType(UInt16Type.Default)
            .Build();
        private ushort _v;
        public ushort Value
        {
            get => _v;
            set => _v = value;
        }

        public void Accept(IVisitor visitor) => UInt16Type.Accept(visitor, Field, ref _v);
    }

    public record UInt32Sub : IVisitable
    {
        public static readonly Field Field = new Field.Builder()
            .Name(nameof(Value))
            .DataType(UInt32Type.Default)
            .Build();
        private uint _v;
        public uint Value
        {
            get => _v;
            set => _v = value;
        }

        public void Accept(IVisitor visitor) => UInt32Type.Accept(visitor, Field, ref _v);
    }

    public record UInt64Sub : IVisitable
    {
        public static readonly Field Field = new Field.Builder()
            .Name(nameof(Value))
            .DataType(UInt64Type.Default)
            .Build();
        private ulong _v;
        public ulong Value
        {
            get => _v;
            set => _v = value;
        }

        public void Accept(IVisitor visitor) => UInt64Type.Accept(visitor, Field, ref _v);
    }

    public record FloatSub : IVisitable
    {
        public static readonly Field Field = new Field.Builder()
            .Name(nameof(Value))
            .DataType(FloatType.Default)
            .Build();
        private float _v;
        public float Value
        {
            get => _v;
            set => _v = value;
        }

        public void Accept(IVisitor visitor) => FloatType.Accept(visitor, Field, ref _v);
    }

    public record DoubleSub : IVisitable
    {
        public static readonly Field Field = new Field.Builder()
            .Name(nameof(Value))
            .DataType(DoubleType.Default)
            .Build();
        private double _v;
        public double Value
        {
            get => _v;
            set => _v = value;
        }

        public void Accept(IVisitor visitor) => DoubleType.Accept(visitor, Field, ref _v);
    }

    // --- Основная структура с: скаляр + массив + вложенная структура для каждого из 12 типов ---
    public record SupportedTypesWithArraysAndSubs : IVisitable
    {
        public const int ArrLen = 4; // удобная фиксированная длина массива для тестов

        // Bool
        public static readonly Field BoolField = new Field.Builder()
            .Name(nameof(BoolValue))
            .DataType(BoolType.Default)
            .Build();
        private bool _boolValue;
        public bool BoolValue
        {
            get => _boolValue;
            set => _boolValue = value;
        }

        public static readonly Field BoolArrayField = new Field.Builder()
            .Name(nameof(BoolArray))
            .DataType(new ArrayType(BoolType.Default, ArrLen))
            .Build();
        private bool[] _boolArray = new bool[ArrLen];
        public bool[] BoolArray
        {
            get => _boolArray;
            set => _boolArray = value;
        }

        public static readonly Field BoolSubField = new Field.Builder()
            .Name(nameof(BoolSubValue))
            .DataType(SubObject.StructType)
            .Build();
        private BoolSub _boolSub = new();
        public BoolSub BoolSubValue
        {
            get => _boolSub;
            set => _boolSub = value;
        }

        // Char
        public static readonly Field CharField = new Field.Builder()
            .Name(nameof(CharValue))
            .DataType(CharType.Ascii)
            .Build();
        private char _charValue;
        public char CharValue
        {
            get => _charValue;
            set => _charValue = value;
        }

        public static readonly Field CharArrayField = new Field.Builder()
            .Name(nameof(CharArray))
            .DataType(new ArrayType(CharType.Ascii, ArrLen))
            .Build();
        private char[] _charArray = new char[ArrLen];
        public char[] CharArray
        {
            get => _charArray;
            set => _charArray = value;
        }

        public static readonly Field CharSubField = new Field.Builder()
            .Name(nameof(CharSubValue))
            .DataType(SubObject.StructType)
            .Build();
        private CharSub _charSub = new();
        public CharSub CharSubValue
        {
            get => _charSub;
            set => _charSub = value;
        }

        // sbyte
        public static readonly Field Int8Field = new Field.Builder()
            .Name(nameof(Int8Value))
            .DataType(Int8Type.Default)
            .Build();
        private sbyte _i8;
        public sbyte Int8Value
        {
            get => _i8;
            set => _i8 = value;
        }

        public static readonly Field Int8ArrayField = new Field.Builder()
            .Name(nameof(Int8Array))
            .DataType(new ArrayType(Int8Type.Default, ArrLen))
            .Build();
        private sbyte[] _i8Arr = new sbyte[ArrLen];
        public sbyte[] Int8Array
        {
            get => _i8Arr;
            set => _i8Arr = value;
        }

        public static readonly Field Int8SubField = new Field.Builder()
            .Name(nameof(Int8SubValue))
            .DataType(SubObject.StructType)
            .Build();
        private Int8Sub _i8Sub = new();
        public Int8Sub Int8SubValue
        {
            get => _i8Sub;
            set => _i8Sub = value;
        }

        // short
        public static readonly Field Int16Field = new Field.Builder()
            .Name(nameof(Int16Value))
            .DataType(Int16Type.Default)
            .Build();
        private short _i16;
        public short Int16Value
        {
            get => _i16;
            set => _i16 = value;
        }

        public static readonly Field Int16ArrayField = new Field.Builder()
            .Name(nameof(Int16Array))
            .DataType(new ArrayType(Int16Type.Default, ArrLen))
            .Build();
        private short[] _i16Arr = new short[ArrLen];
        public short[] Int16Array
        {
            get => _i16Arr;
            set => _i16Arr = value;
        }

        public static readonly Field Int16SubField = new Field.Builder()
            .Name(nameof(Int16SubValue))
            .DataType(SubObject.StructType)
            .Build();
        private Int16Sub _i16Sub = new();
        public Int16Sub Int16SubValue
        {
            get => _i16Sub;
            set => _i16Sub = value;
        }

        // int
        public static readonly Field Int32Field = new Field.Builder()
            .Name(nameof(Int32Value))
            .DataType(Int32Type.Default)
            .Build();
        private int _i32;
        public int Int32Value
        {
            get => _i32;
            set => _i32 = value;
        }

        public static readonly Field Int32ArrayField = new Field.Builder()
            .Name(nameof(Int32Array))
            .DataType(new ArrayType(Int32Type.Default, ArrLen))
            .Build();
        private int[] _i32Arr = new int[ArrLen];
        public int[] Int32Array
        {
            get => _i32Arr;
            set => _i32Arr = value;
        }

        public static readonly Field Int32SubField = new Field.Builder()
            .Name(nameof(Int32SubValue))
            .DataType(SubObject.StructType)
            .Build();
        private Int32Sub _i32Sub = new();
        public Int32Sub Int32SubValue
        {
            get => _i32Sub;
            set => _i32Sub = value;
        }

        // long
        public static readonly Field Int64Field = new Field.Builder()
            .Name(nameof(Int64Value))
            .DataType(Int64Type.Default)
            .Build();
        private long _i64;
        public long Int64Value
        {
            get => _i64;
            set => _i64 = value;
        }

        public static readonly Field Int64ArrayField = new Field.Builder()
            .Name(nameof(Int64Array))
            .DataType(new ArrayType(Int64Type.Default, ArrLen))
            .Build();
        private long[] _i64Arr = new long[ArrLen];
        public long[] Int64Array
        {
            get => _i64Arr;
            set => _i64Arr = value;
        }

        public static readonly Field Int64SubField = new Field.Builder()
            .Name(nameof(Int64SubValue))
            .DataType(SubObject.StructType)
            .Build();
        private Int64Sub _i64Sub = new();
        public Int64Sub Int64SubValue
        {
            get => _i64Sub;
            set => _i64Sub = value;
        }

        // byte
        public static readonly Field UInt8Field = new Field.Builder()
            .Name(nameof(UInt8Value))
            .DataType(UInt8Type.Default)
            .Build();
        private byte _u8;
        public byte UInt8Value
        {
            get => _u8;
            set => _u8 = value;
        }

        public static readonly Field UInt8ArrayField = new Field.Builder()
            .Name(nameof(UInt8Array))
            .DataType(new ArrayType(UInt8Type.Default, ArrLen))
            .Build();
        private byte[] _u8Arr = new byte[ArrLen];
        public byte[] UInt8Array
        {
            get => _u8Arr;
            set => _u8Arr = value;
        }

        public static readonly Field UInt8SubField = new Field.Builder()
            .Name(nameof(UInt8SubValue))
            .DataType(SubObject.StructType)
            .Build();
        private UInt8Sub _u8Sub = new();
        public UInt8Sub UInt8SubValue
        {
            get => _u8Sub;
            set => _u8Sub = value;
        }

        // ushort
        public static readonly Field UInt16Field = new Field.Builder()
            .Name(nameof(UInt16Value))
            .DataType(UInt16Type.Default)
            .Build();
        private ushort _u16;
        public ushort UInt16Value
        {
            get => _u16;
            set => _u16 = value;
        }

        public static readonly Field UInt16ArrayField = new Field.Builder()
            .Name(nameof(UInt16Array))
            .DataType(new ArrayType(UInt16Type.Default, ArrLen))
            .Build();
        private ushort[] _u16Arr = new ushort[ArrLen];
        public ushort[] UInt16Array
        {
            get => _u16Arr;
            set => _u16Arr = value;
        }

        public static readonly Field UInt16SubField = new Field.Builder()
            .Name(nameof(UInt16SubValue))
            .DataType(SubObject.StructType)
            .Build();
        private UInt16Sub _u16Sub = new();
        public UInt16Sub UInt16SubValue
        {
            get => _u16Sub;
            set => _u16Sub = value;
        }

        // uint
        public static readonly Field UInt32Field = new Field.Builder()
            .Name(nameof(UInt32Value))
            .DataType(UInt32Type.Default)
            .Build();
        private uint _u32;
        public uint UInt32Value
        {
            get => _u32;
            set => _u32 = value;
        }

        public static readonly Field UInt32ArrayField = new Field.Builder()
            .Name(nameof(UInt32Array))
            .DataType(new ArrayType(UInt32Type.Default, ArrLen))
            .Build();
        private uint[] _u32Arr = new uint[ArrLen];
        public uint[] UInt32Array
        {
            get => _u32Arr;
            set => _u32Arr = value;
        }

        public static readonly Field UInt32SubField = new Field.Builder()
            .Name(nameof(UInt32SubValue))
            .DataType(SubObject.StructType)
            .Build();
        private UInt32Sub _u32Sub = new();
        public UInt32Sub UInt32SubValue
        {
            get => _u32Sub;
            set => _u32Sub = value;
        }

        // ulong
        public static readonly Field UInt64Field = new Field.Builder()
            .Name(nameof(UInt64Value))
            .DataType(UInt64Type.Default)
            .Build();
        private ulong _u64;
        public ulong UInt64Value
        {
            get => _u64;
            set => _u64 = value;
        }

        public static readonly Field UInt64ArrayField = new Field.Builder()
            .Name(nameof(UInt64Array))
            .DataType(new ArrayType(UInt64Type.Default, ArrLen))
            .Build();
        private ulong[] _u64Arr = new ulong[ArrLen];
        public ulong[] UInt64Array
        {
            get => _u64Arr;
            set => _u64Arr = value;
        }

        public static readonly Field UInt64SubField = new Field.Builder()
            .Name(nameof(UInt64SubValue))
            .DataType(SubObject.StructType)
            .Build();
        private UInt64Sub _u64Sub = new();
        public UInt64Sub UInt64SubValue
        {
            get => _u64Sub;
            set => _u64Sub = value;
        }

        // float
        public static readonly Field FloatField = new Field.Builder()
            .Name(nameof(FloatValue))
            .DataType(FloatType.Default)
            .Build();
        private float _f;
        public float FloatValue
        {
            get => _f;
            set => _f = value;
        }

        public static readonly Field FloatArrayField = new Field.Builder()
            .Name(nameof(FloatArray))
            .DataType(new ArrayType(FloatType.Default, ArrLen))
            .Build();
        private float[] _fArr = new float[ArrLen];
        public float[] FloatArray
        {
            get => _fArr;
            set => _fArr = value;
        }

        public static readonly Field FloatSubField = new Field.Builder()
            .Name(nameof(FloatSubValue))
            .DataType(SubObject.StructType)
            .Build();
        private FloatSub _fSub = new();
        public FloatSub FloatSubValue
        {
            get => _fSub;
            set => _fSub = value;
        }

        // double
        public static readonly Field DoubleField = new Field.Builder()
            .Name(nameof(DoubleValue))
            .DataType(DoubleType.Default)
            .Build();
        private double _d;
        public double DoubleValue
        {
            get => _d;
            set => _d = value;
        }

        public static readonly Field DoubleArrayField = new Field.Builder()
            .Name(nameof(DoubleArray))
            .DataType(new ArrayType(DoubleType.Default, ArrLen))
            .Build();
        private double[] _dArr = new double[ArrLen];
        public double[] DoubleArray
        {
            get => _dArr;
            set => _dArr = value;
        }

        public static readonly Field DoubleSubField = new Field.Builder()
            .Name(nameof(DoubleSubValue))
            .DataType(SubObject.StructType)
            .Build();
        private DoubleSub _dSub = new();
        public DoubleSub DoubleSubValue
        {
            get => _dSub;
            set => _dSub = value;
        }

        public void Accept(IVisitor visitor)
        {
            // Bool
            BoolType.Accept(visitor, BoolField, ref _boolValue);
            ArrayType.Accept(
                visitor,
                BoolArrayField,
                (i, v, f, t) => BoolType.Accept(v, f, t, ref _boolArray[i])
            );
            StructType.Accept(visitor, BoolSubField, _boolSub);

            // Char
            CharType.Accept(visitor, CharField, ref _charValue);
            ArrayType.Accept(
                visitor,
                CharArrayField,
                (i, v, f, t) => CharType.Accept(v, f, t, ref _charArray[i])
            );
            StructType.Accept(visitor, CharSubField, _charSub);

            // Int8
            Int8Type.Accept(visitor, Int8Field, ref _i8);
            ArrayType.Accept(
                visitor,
                Int8ArrayField,
                (i, v, f, t) => Int8Type.Accept(v, f, t, ref _i8Arr[i])
            );
            StructType.Accept(visitor, Int8SubField, _i8Sub);

            // Int16
            Int16Type.Accept(visitor, Int16Field, ref _i16);
            ArrayType.Accept(
                visitor,
                Int16ArrayField,
                (i, v, f, t) => Int16Type.Accept(v, f, t, ref _i16Arr[i])
            );
            StructType.Accept(visitor, Int16SubField, _i16Sub);

            // Int32
            Int32Type.Accept(visitor, Int32Field, ref _i32);
            ArrayType.Accept(
                visitor,
                Int32ArrayField,
                (i, v, f, t) => Int32Type.Accept(v, f, t, ref _i32Arr[i])
            );
            StructType.Accept(visitor, Int32SubField, _i32Sub);

            // Int64
            Int64Type.Accept(visitor, Int64Field, ref _i64);
            ArrayType.Accept(
                visitor,
                Int64ArrayField,
                (i, v, f, t) => Int64Type.Accept(v, f, t, ref _i64Arr[i])
            );
            StructType.Accept(visitor, Int64SubField, _i64Sub);

            // UInt8
            UInt8Type.Accept(visitor, UInt8Field, ref _u8);
            ArrayType.Accept(
                visitor,
                UInt8ArrayField,
                (i, v, f, t) => UInt8Type.Accept(v, f, t, ref _u8Arr[i])
            );
            StructType.Accept(visitor, UInt8SubField, _u8Sub);

            // UInt16
            UInt16Type.Accept(visitor, UInt16Field, ref _u16);
            ArrayType.Accept(
                visitor,
                UInt16ArrayField,
                (i, v, f, t) => UInt16Type.Accept(v, f, t, ref _u16Arr[i])
            );
            StructType.Accept(visitor, UInt16SubField, _u16Sub);

            // UInt32
            UInt32Type.Accept(visitor, UInt32Field, ref _u32);
            ArrayType.Accept(
                visitor,
                UInt32ArrayField,
                (i, v, f, t) => UInt32Type.Accept(v, f, t, ref _u32Arr[i])
            );
            StructType.Accept(visitor, UInt32SubField, _u32Sub);

            // UInt64
            UInt64Type.Accept(visitor, UInt64Field, ref _u64);
            ArrayType.Accept(
                visitor,
                UInt64ArrayField,
                (i, v, f, t) => UInt64Type.Accept(v, f, t, ref _u64Arr[i])
            );
            StructType.Accept(visitor, UInt64SubField, _u64Sub);

            // Float
            FloatType.Accept(visitor, FloatField, ref _f);
            ArrayType.Accept(
                visitor,
                FloatArrayField,
                (i, v, f, t) => FloatType.Accept(v, f, t, ref _fArr[i])
            );
            StructType.Accept(visitor, FloatSubField, _fSub);

            // Double
            DoubleType.Accept(visitor, DoubleField, ref _d);
            ArrayType.Accept(
                visitor,
                DoubleArrayField,
                (i, v, f, t) => DoubleType.Accept(v, f, t, ref _dArr[i])
            );
            StructType.Accept(visitor, DoubleSubField, _dSub);
        }
    }
}
