using System;
using System.Buffers;

namespace Asv.IO;

public class SimpleBinarySerialize(IBufferWriter<byte> buffer, bool skipUnknown) : FullVisitorBase(skipUnknown)
{
    public override void Visit(Field field, UInt8Type type, ref byte value)
    {
       BinSerialize.WriteByte(buffer, value);
    }

    public override void Visit(Field field, HalfFloatType type, ref Half value)
    {
        var floatValue = (float)value;
        BinSerialize.WriteFloat(buffer, floatValue);
    }

    public override void Visit(Field field, Int8Type type, ref sbyte value)
    {
        BinSerialize.WriteSByte(buffer, value);
    }

    public override void Visit(Field field, Int16Type type, ref short value)
    {
        BinSerialize.WriteShort(buffer, value);
    }

    public override void Visit(Field field, UInt16Type type, ref ushort value)
    {
        BinSerialize.WriteUShort(buffer, value);
    }

    public override void Visit(Field field, Int32Type type, ref int value)
    {
        BinSerialize.WriteInt(buffer, value);
    }

    public override void Visit(Field field, UInt32Type type,ref uint value)
    {
        BinSerialize.WriteUInt(buffer, value);
    }

    public override void Visit(Field field, Int64Type type, ref long value)
    {
        BinSerialize.WriteLong(buffer, value);
    }

    public override void Visit(Field field, UInt64Type type, ref ulong value)
    {
        BinSerialize.WriteULong(buffer, value);
    }

    public override void Visit(Field field, FloatType type, ref float value)
    {
        BinSerialize.WriteFloat(buffer, value);
    }

    public override void Visit(Field field, DoubleOptionalType type, ref double? value)
    {
        if (value.HasValue)
        {
            BinSerialize.WriteBool(buffer, true);
            BinSerialize.WriteDouble(buffer, value.Value);
        }
        else
        {
            BinSerialize.WriteBool(buffer, false);
        }
    }

    public override void Visit(Field field, FloatOptionalType type, ref float? value)
    {
        if (value.HasValue)
        {
            BinSerialize.WriteBool(buffer, true);
            BinSerialize.WriteFloat(buffer, value.Value);
        }
        else
        {
            BinSerialize.WriteBool(buffer, false);
        }
    }

    public override void Visit(Field field, HalfFloatOptionalType type, ref Half? value)
    {
        if (value.HasValue)
        {
            BinSerialize.WriteBool(buffer, true);
            var floatValue = (float)value.Value;
            BinSerialize.WriteFloat(buffer, floatValue);
        }
        else
        {
            BinSerialize.WriteBool(buffer, false);
        }
    }

    public override void Visit(Field field, Int8OptionalType type, ref sbyte? value)
    {
        if (value.HasValue)
        {
            BinSerialize.WriteBool(buffer, true);
            BinSerialize.WriteSByte(buffer, value.Value);
        }
        else
        {
            BinSerialize.WriteBool(buffer, false);
        }
    }

    public override void Visit(Field field, Int16OptionalType type, ref short? value)
    {
        if (value.HasValue)
        {
            BinSerialize.WriteBool(buffer, true);
            BinSerialize.WriteShort(buffer, value.Value);
        }
        else
        {
            BinSerialize.WriteBool(buffer, false);
        }
    }

    public override void Visit(Field field, Int32OptionalType type, ref int? value)
    {
        if (value.HasValue)
        {
            BinSerialize.WriteBool(buffer, true);
            BinSerialize.WriteInt(buffer, value.Value);
        }
        else
        {
            BinSerialize.WriteBool(buffer, false);
        }
    }

    public override void Visit(Field field, Int64OptionalType type, ref long? value)
    {
        if (value.HasValue)
        {
            BinSerialize.WriteBool(buffer, true);
            BinSerialize.WriteLong(buffer, value.Value);
        }
        else
        {
            BinSerialize.WriteBool(buffer, false);
        }
    }

    public override void Visit(Field field, UInt8OptionalType type, ref byte? value)
    {
        if (value.HasValue)
        {
            BinSerialize.WriteBool(buffer, true);
            BinSerialize.WriteByte(buffer, value.Value);
        }
        else
        {
            BinSerialize.WriteBool(buffer, false);
        }
    }

    public override void Visit(Field field, UInt16OptionalType type, ref ushort? value)
    {
        if (value.HasValue)
        {
            BinSerialize.WriteBool(buffer, true);
            BinSerialize.WriteUShort(buffer, value.Value);
        }
        else
        {
            BinSerialize.WriteBool(buffer, false);
        }
    }

    public override void Visit(Field field, UInt32OptionalType type, ref uint? value)
    {
        if (value.HasValue)
        {
            BinSerialize.WriteBool(buffer, true);
            BinSerialize.WriteUInt(buffer, value.Value);
        }
        else
        {
            BinSerialize.WriteBool(buffer, false);
        }
    }

    public override void Visit(Field field, UInt64OptionalType type, ref ulong? value)
    {
        if (value.HasValue)
        {
            BinSerialize.WriteBool(buffer, true);
            BinSerialize.WriteULong(buffer, value.Value);
        }
        else
        {
            BinSerialize.WriteBool(buffer, false);
        }
    }

    public override void Visit(Field field, StringOptionalType type, ref string? value)
    {
        if (value != null)
        {
            BinSerialize.WriteBool(buffer, true);
            BinSerialize.WriteString(buffer, value);
        }
        else
        {
            BinSerialize.WriteBool(buffer, false);
        }
    }

    public override void Visit(Field field, BoolOptionalType type, ref bool? value)
    {
        if (value.HasValue)
        {
            BinSerialize.WriteBool(buffer, true);
            BinSerialize.WriteBool(buffer, value.Value);
        }
        else
        {
            BinSerialize.WriteBool(buffer, false);
        }
    }

    public override void Visit(Field field, CharOptionalType type, ref char? value)
    {
        if (value.HasValue)
        {
            BinSerialize.WriteBool(buffer, true);
            BinSerialize.WriteByte(buffer, (byte)value.Value);
        }
        else
        {
            BinSerialize.WriteBool(buffer, false);
        }
    }

    public override void Visit(Field field, DateTimeType type, ref DateTime value)
    {
        BinSerialize.WriteLong(buffer, value.ToBinary());
    }

    public override void Visit(Field field, DateTimeOptionalType type, ref DateTime? value)
    {
        if (value.HasValue)
        {
            BinSerialize.WriteBool(buffer, true);
            BinSerialize.WriteLong(buffer, value.Value.ToBinary());
        }
        else
        {
            BinSerialize.WriteBool(buffer, false);
        }
    }

    public override void Visit(Field field, TimeSpanType type, ref TimeSpan value)
    {
        BinSerialize.WriteLong(buffer, value.Ticks);
    }

    public override void Visit(Field field, TimeSpanOptionalType type, ref TimeSpan? value)
    {
        if (value.HasValue)
        {
            BinSerialize.WriteBool(buffer, true);
            BinSerialize.WriteLong(buffer, value.Value.Ticks);
        }
        else
        {
            BinSerialize.WriteBool(buffer, false);
        }
    }

    public override void Visit(Field field, DateOnlyType type, ref DateOnly value)
    {
        BinSerialize.WriteUShort(buffer, (ushort)value.Year);
        BinSerialize.WriteByte(buffer, (byte)value.Month);
        BinSerialize.WriteByte(buffer, (byte)value.Day);
    }

    public override void Visit(Field field, DateOnlyOptionalType type, ref DateOnly? value)
    {
        if (value.HasValue)
        {
            BinSerialize.WriteBool(buffer, true);
            BinSerialize.WriteUShort(buffer, (ushort)value.Value.Year);
            BinSerialize.WriteByte(buffer, (byte)value.Value.Month);
            BinSerialize.WriteByte(buffer, (byte)value.Value.Day);
        }
        else
        {
            BinSerialize.WriteBool(buffer, false);
        }
    }

    public override void Visit(Field field, TimeOnlyType type, ref TimeOnly value)
    {
        BinSerialize.WriteByte(buffer, (byte)value.Hour);
        BinSerialize.WriteByte(buffer, (byte)value.Minute);
        BinSerialize.WriteByte(buffer, (byte)value.Second);
        BinSerialize.WriteByte(buffer, (byte)value.Millisecond);
    }

    public override void Visit(Field field, TimeOnlyOptionalType type, ref TimeOnly? value)
    {
        if (value.HasValue)
        {
            BinSerialize.WriteBool(buffer, true);
            BinSerialize.WriteByte(buffer, (byte)value.Value.Hour);
            BinSerialize.WriteByte(buffer, (byte)value.Value.Minute);
            BinSerialize.WriteByte(buffer, (byte)value.Value.Second);
            BinSerialize.WriteByte(buffer, (byte)value.Value.Millisecond);
        }
        else
        {
            BinSerialize.WriteBool(buffer, false);
        }
    }

    public override void Visit(Field field, DoubleType type, ref double value)
    {
        BinSerialize.WriteDouble(buffer, value);
    }

    public override void Visit(Field field, StringType type, ref string value)
    {
        BinSerialize.WriteString(buffer, value);
    }

    public override void Visit(Field field, BoolType type, ref bool value)
    {
        BinSerialize.WriteBool(buffer, value);
    }

    public override void Visit(Field field, CharType type, ref char value)
    {
        BinSerialize.WriteByte(buffer, (byte)value);
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
        BinSerialize.WriteBool(buffer, isPresent);
        createNew = false; 
    }

    public override void EndOptionalStruct(bool isPresent)
    {
        // fixed size struct => skip
    }

    public override void BeginList(Field field, ListType type, ref uint size)
    {
        BinSerialize.WriteUInt(buffer, size);
    }

    public override void EndList()
    {
        
    }
}