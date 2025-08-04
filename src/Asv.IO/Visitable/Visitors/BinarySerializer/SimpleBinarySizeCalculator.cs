using System;

namespace Asv.IO;

public class SimpleBinarySizeCalculator(bool skipUnknown) : FullVisitorBase(skipUnknown)
{
    public int Size { get; private set; }

    public override void Visit(Field field, UInt8Type type,  ref byte value)
    {
        Size += sizeof(byte);
    }

    public override void Visit(Field field, HalfFloatType type, ref Half value)
    {
        Size += sizeof(float);
    }

    public override void Visit(Field field, Int8Type type, ref sbyte value)
    {
        Size += sizeof(sbyte);
    }

    public override void Visit(Field field, Int16Type type, ref short value)
    {
        Size += sizeof(short);
    }

    public override void Visit(Field field, UInt16Type type, ref ushort value)
    {
        Size += sizeof(ushort);
    }

    public override void Visit(Field field, Int32Type type, ref int value)
    {
        Size += sizeof(int);
    }

    public override void Visit(Field field, UInt32Type type, ref uint value)
    {
        Size += sizeof(uint);
    }

    public override void Visit(Field field, Int64Type type, ref long value)
    {
        Size += sizeof(long);
    }

    public override void Visit(Field field, UInt64Type type, ref ulong value)
    {
        Size += sizeof(ulong);
    }

    public override void Visit(Field field, FloatType type, ref float value)
    {
        Size += sizeof(float);
    }

    public override void Visit(Field field, DoubleOptionalType type, ref double? value)
    {
        if (value.HasValue)
        {
            Size += sizeof(bool) + sizeof(double);
            
        }
        else
        {
            Size += sizeof(bool);
        }
    }

    public override void Visit(Field field, FloatOptionalType type, ref float? value)
    {
        if (value.HasValue)
        {
            Size += sizeof(bool) + sizeof(float);
        }
        else
        {
            Size += sizeof(bool);
        }
    }

    public override void Visit(Field field, HalfFloatOptionalType type, ref Half? value)
    {
        if (value.HasValue)
        {
            Size += sizeof(bool) + sizeof(float); // Half is stored as float
        }
        else
        {
            Size += sizeof(bool);
        }
    }

    public override void Visit(Field field, Int8OptionalType type, ref sbyte? value)
    {
        if (value.HasValue)
        {
            Size += sizeof(bool) + sizeof(sbyte);
        }
        else
        {
            Size += sizeof(bool);
        }
    }

    public override void Visit(Field field, Int16OptionalType type, ref short? value)
    {
        if (value.HasValue)
        {
            Size += sizeof(bool) + sizeof(short);
        }
        else
        {
            Size += sizeof(bool);
        }
    }

    public override void Visit(Field field, Int32OptionalType type, ref int? value)
    {
        if (value.HasValue)
        {
            Size += sizeof(bool) + sizeof(int);
        }
        else
        {
            Size += sizeof(bool);
        }
    }

    public override void Visit(Field field, Int64OptionalType type, ref long? value)
    {
        if (value.HasValue)
        {
            Size += sizeof(bool) + sizeof(long);
        }
        else
        {
            Size += sizeof(bool);
        }
    }

    public override void Visit(Field field, UInt8OptionalType type, ref byte? value)
    {
        if (value.HasValue)
        {
            Size += sizeof(bool) + sizeof(byte);
        }
        else
        {
            Size += sizeof(bool);
        }
    }

    public override void Visit(Field field, UInt16OptionalType type, ref ushort? value)
    {
        if (value.HasValue)
        {
            Size += sizeof(bool) + sizeof(ushort);
        }
        else
        {
            Size += sizeof(bool);
        }
    }

    public override void Visit(Field field, UInt32OptionalType type, ref uint? value)
    {
        if (value.HasValue)
        {
            Size += sizeof(bool) + sizeof(uint);
        }
        else
        {
            Size += sizeof(bool);
        }
    }

    public override void Visit(Field field, UInt64OptionalType type, ref ulong? value)
    {
        if (value.HasValue)
        {
            Size += sizeof(bool) + sizeof(ulong);
        }
        else
        {
            Size += sizeof(bool);
        }
    }

    public override void Visit(Field field, StringOptionalType type, ref string? value)
    {
        if (value != null)
        {
            Size += sizeof(bool) + BinSerialize.GetSizeForString(value);
        }
        else
        {
            Size += sizeof(bool);
        }
    }

    public override void Visit(Field field, BoolOptionalType type, ref bool? value)
    {
        if (value.HasValue)
        {
            Size += sizeof(bool) + sizeof(bool);
        }
        else
        {
            Size += sizeof(bool);
        }
    }

    public override void Visit(Field field, CharOptionalType type, ref char? value)
    {
        if (value.HasValue)
        {
            if (type.Encoding == EncodingId.Ascii)
            {
                Size += sizeof(bool) + 1; // 1 byte for ASCII char
            }
            else
            {
                throw new NotImplementedException($"Encoding {type.Encoding} is not supported");
            }
        }
        else
        {
            Size += sizeof(bool);
        }
    }

    public override void Visit(Field field, DateTimeType type, ref DateTime value)
    {
        Size += sizeof(long);
    }

    public override void Visit(Field field, DateTimeOptionalType type, ref DateTime? value)
    {
        if (value.HasValue)
        {
            Size += sizeof(bool) + sizeof(long);
        }
        else
        {
            Size += sizeof(bool);
        }
    }

    public override void Visit(Field field, TimeSpanType type, ref TimeSpan value)
    {
        Size += sizeof(long); // TimeSpan is stored as ticks (long)
    }

    public override void Visit(Field field, TimeSpanOptionalType type, ref TimeSpan? value)
    {
        if (value.HasValue)
        {
            Size += sizeof(bool) + sizeof(long); // TimeSpan is stored as ticks (long)
        }
        else
        {
            Size += sizeof(bool);
        }
    }

    public override void Visit(Field field, DateOnlyType type, ref DateOnly value)
    {
        Size += sizeof(ushort) + sizeof(byte) + sizeof(byte);
    }

    public override void Visit(Field field, DateOnlyOptionalType type, ref DateOnly? value)
    {
        if (value.HasValue)
        {
            Size += sizeof(bool) + sizeof(ushort) + sizeof(byte) + sizeof(byte);
        }
        else
        {
            Size += sizeof(bool);
        }
    }

    public override void Visit(Field field, TimeOnlyType type, ref TimeOnly value)
    {
        Size += sizeof(byte) + sizeof(byte) + sizeof(byte); // Hour, Minute, Second
    }

    public override void Visit(Field field, TimeOnlyOptionalType type, ref TimeOnly? value)
    {
        if (value.HasValue)
        {
            Size += sizeof(bool) + sizeof(byte) + sizeof(byte) + sizeof(byte); // Hour, Minute, Second
        }
        else
        {
            Size += sizeof(bool);
        }
    }

    public override void Visit(Field field, DoubleType type, ref double value)
    {
        Size += sizeof(double);
    }

    public override void Visit(Field field, StringType type, ref string value)
    {
        Size+= BinSerialize.GetSizeForString(value);
    }

    public override void Visit(Field field, BoolType type, ref bool value)
    {
        Size += sizeof(bool);
    }

    public override void Visit(Field field, CharType type, ref char value)
    {
        if (type.Encoding == EncodingId.Ascii)
        {
            Size += 1;    
        }
        else
        {
            throw new NotImplementedException($"Encoding {type.Encoding} is not supported");
        }
        
    }

    public override void BeginArray(Field field, ArrayType type)
    {
        // fixed size array => skip
    }

    public override void EndArray()
    {
        // fixed size array => skip
    }

    public override void BeginStruct(Field field, StructType type)
    {
        // fixed size struct => skip
    }

    public override void EndStruct()
    {
        // fixed size struct => skip
    }

    public override void BeginOptionalStruct(Field field, OptionalStructType type, bool isPresent, out bool createNew)
    {
        createNew = false; // We do not create a new struct, we just calculate size
        Size += sizeof(bool); // Add size for presence flag
    }

    public override void EndOptionalStruct(bool isPresent)
    {
        // do nothing, size already calculated
    }

    public override void BeginList(Field field, ListType type, ref uint size)
    {
        Size += sizeof(uint);
    }

    public override void EndList()
    {
        // do nothing
    }
}