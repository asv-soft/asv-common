using System;
using System.Buffers;

namespace Asv.IO;

public delegate void ReadDelegate(ref Span<byte> buffer);

public class SimpleBinaryDeserialize(ReadOnlyMemory<byte> memory, bool skipUnknown) : FullVisitorBase(skipUnknown)
{
    public ReadOnlyMemory<byte> Memory = memory;

    public override void Visit(Field field, HalfFloatType type, ref Half value)
    {
        var temp = 0f;
        BinSerialize.ReadFloat(ref Memory, ref temp);
        value = (Half)temp;
    }

    public override void Visit(Field field, Int8Type type, ref sbyte value)
    {
        BinSerialize.ReadSByte(ref Memory, ref value);
    }

    public override void Visit(Field field, Int16Type type, ref short value)
    {
        BinSerialize.ReadShort(ref Memory, ref value);        
    }

    public override void Visit(Field field,UInt16Type type, ref ushort value)
    {
        BinSerialize.ReadUShort(ref Memory, ref value);
    }

    public override void Visit(Field field, Int32Type type, ref int value)
    {
        BinSerialize.ReadInt(ref Memory, ref value);
    }

    public override void Visit(Field field, UInt32Type type, ref uint value)
    {
        BinSerialize.ReadUInt(ref Memory, ref value);        
    }

    public override void Visit(Field field, Int64Type type, ref long value)
    {
        BinSerialize.ReadLong(ref Memory, ref value);
    }

    public override void Visit(Field field, UInt64Type type, ref ulong value)
    {
        BinSerialize.ReadULong(ref Memory, ref value);
    }

    public override void Visit(Field field, FloatType type, ref float value)
    {
        BinSerialize.ReadFloat(ref Memory, ref value);
    }

    public override void Visit(Field field, DoubleOptionalType type, ref double? value)
    {
        var isPresent = false;
        BinSerialize.ReadBool(ref Memory, ref isPresent);
        if (isPresent)
        {
            var temp = 0.0;
            BinSerialize.ReadDouble(ref Memory, ref temp);
            value = temp;
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, FloatOptionalType type, ref float? value)
    {
        var isPresent = false;
        BinSerialize.ReadBool(ref Memory, ref isPresent);
        if (isPresent)
        {
            var temp = 0f;
            BinSerialize.ReadFloat(ref Memory, ref temp);
            value = temp;
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, HalfFloatOptionalType type, ref Half? value)
    {
        var isPresent = false;
        BinSerialize.ReadBool(ref Memory, ref isPresent);
        if (isPresent)
        {
            var temp = 0.0f;
            BinSerialize.ReadFloat(ref Memory, ref temp);
            value = (Half?)temp;
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, Int8OptionalType type, ref sbyte? value)
    {
        var isPresent = false;
        BinSerialize.ReadBool(ref Memory, ref isPresent);
        if (isPresent)
        {
            var temp = default(sbyte);
            BinSerialize.ReadSByte(ref Memory, ref temp);
            value = temp;
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, Int16OptionalType type, ref short? value)
    {
        var isPresent = false;
        BinSerialize.ReadBool(ref Memory, ref isPresent);
        if (isPresent)
        {
            var temp = default(short);
            BinSerialize.ReadShort(ref Memory, ref temp);
            value = temp;
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, Int32OptionalType type, ref int? value)
    {
        var isPresent = false;
        BinSerialize.ReadBool(ref Memory, ref isPresent);
        if (isPresent)
        {
            var temp = 0;
            BinSerialize.ReadInt(ref Memory, ref temp);
            value = temp;
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, Int64OptionalType type, ref long? value)
    {
        var isPresent = false;
        BinSerialize.ReadBool(ref Memory, ref isPresent);
        if (isPresent)
        {
            var temp = 0L;
            BinSerialize.ReadLong(ref Memory, ref temp);
            value = temp;
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, UInt8OptionalType type, ref byte? value)
    {
        var isPresent = false;
        BinSerialize.ReadBool(ref Memory, ref isPresent);
        if (isPresent)
        {
            var temp = default(byte);
            BinSerialize.ReadByte(ref Memory, ref temp);
            value = temp;
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, UInt16OptionalType type, ref ushort? value)
    {
        var isPresent = false;
        BinSerialize.ReadBool(ref Memory, ref isPresent);
        if (isPresent)
        {
            var temp = default(ushort);
            BinSerialize.ReadUShort(ref Memory, ref temp);
            value = temp;
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, UInt32OptionalType type, ref uint? value)
    {
        var isPresent = false;
        BinSerialize.ReadBool(ref Memory, ref isPresent);
        if (isPresent)
        {
            var temp = 0U;
            BinSerialize.ReadUInt(ref Memory, ref temp);
            value = temp;
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, UInt64OptionalType type, ref ulong? value)
    {
        var isPresent = false;
        BinSerialize.ReadBool(ref Memory, ref isPresent);
        if (isPresent)
        {
            var temp = 0UL;
            BinSerialize.ReadULong(ref Memory, ref temp);
            value = temp;
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, StringOptionalType type, ref string? value)
    {
        var isPresent = false;
        BinSerialize.ReadBool(ref Memory, ref isPresent);
        if (isPresent)
        {
            value = BinSerialize.ReadString(ref Memory);
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, BoolOptionalType type, ref bool? value)
    {
        var isPresent = false;
        BinSerialize.ReadBool(ref Memory, ref isPresent);
        if (isPresent)
        {
            var temp = false;
            BinSerialize.ReadBool(ref Memory, ref temp);
            value = temp;
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, CharOptionalType type, ref char? value)
    {
        var isPresent = false;
        BinSerialize.ReadBool(ref Memory, ref isPresent);
        if (isPresent)
        {
            var tmp = default(byte);
            BinSerialize.ReadByte(ref Memory, ref tmp);
            value = (char)tmp;
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, DateTimeType type, ref DateTime value)
    {
        long temp = 0;
        BinSerialize.ReadLong(ref Memory, ref temp);
        value = DateTime.FromBinary(temp);
    }

    public override void Visit(Field field, DateTimeOptionalType type, ref DateTime? value)
    {
        var isPresent = false;
        BinSerialize.ReadBool(ref Memory, ref isPresent);
        if (isPresent)
        {
            long temp = 0;
            BinSerialize.ReadLong(ref Memory, ref temp);
            value = DateTime.FromBinary(temp);
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, TimeSpanType type, ref TimeSpan value)
    {
        long temp = 0;
        BinSerialize.ReadLong(ref Memory, ref temp);
        value = TimeSpan.FromTicks(temp);
    }

    public override void Visit(Field field, TimeSpanOptionalType type, ref TimeSpan? value)
    {
        var isPresent = false;
        BinSerialize.ReadBool(ref Memory, ref isPresent);
        if (isPresent)
        {
            long temp = 0;
            BinSerialize.ReadLong(ref Memory, ref temp);
            value = TimeSpan.FromTicks(temp);
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, DateOnlyType type, ref DateOnly value)
    {
        ushort year = 0;
        byte month = 0;
        byte day = 0;
        BinSerialize.ReadUShort(ref Memory, ref year);
        BinSerialize.ReadByte(ref Memory, ref month);
        BinSerialize.ReadByte(ref Memory, ref day);
        value = new DateOnly(year, month, day);
    }

    public override void Visit(Field field, DateOnlyOptionalType type, ref DateOnly? value)
    {
        var isPresent = false;
        BinSerialize.ReadBool(ref Memory, ref isPresent);
        if (isPresent)
        {
            ushort year = 0;
            byte month = 0;
            byte day = 0;
            BinSerialize.ReadUShort(ref Memory, ref year);
            BinSerialize.ReadByte(ref Memory, ref month);
            BinSerialize.ReadByte(ref Memory, ref day);
            value = new DateOnly(year, month, day);
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, TimeOnlyType type, ref TimeOnly value)
    {
        byte hour = 0;
        byte minute = 0;
        byte second = 0;
        byte millisecond = 0;
        BinSerialize.ReadByte(ref Memory, ref hour);
        BinSerialize.ReadByte(ref Memory, ref minute);
        BinSerialize.ReadByte(ref Memory, ref second);
        BinSerialize.ReadByte(ref Memory, ref millisecond);
        value = new TimeOnly(hour, minute, second, millisecond);
    }

    public override void Visit(Field field, TimeOnlyOptionalType type, ref TimeOnly? value)
    {
        var isPresent = false;
        BinSerialize.ReadBool(ref Memory, ref isPresent);
        if (isPresent)
        {
            byte hour = 0;
            byte minute = 0;
            byte second = 0;
            byte millisecond = 0;
            BinSerialize.ReadByte(ref Memory, ref hour);
            BinSerialize.ReadByte(ref Memory, ref minute);
            BinSerialize.ReadByte(ref Memory, ref second);
            BinSerialize.ReadByte(ref Memory, ref millisecond);
            value = new TimeOnly(hour, minute, second, millisecond);
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, DoubleType type, ref double value)
    {
        BinSerialize.ReadDouble(ref Memory, ref value);
    }

    public override void Visit(Field field, StringType type, ref string value)
    {
        value = BinSerialize.ReadString(ref Memory);
    }

    public override void Visit(Field field, BoolType type, ref bool value)
    {
        BinSerialize.ReadBool(ref Memory, ref value);
    }

    public override void Visit(Field field, CharType type, ref char value)
    {
        var tmp = default(byte);
        BinSerialize.ReadByte(ref Memory, ref tmp);
        value = (char)tmp;
    }

    public override void Visit(Field field, UInt8Type type, ref byte value)
    {
        BinSerialize.ReadByte(ref Memory, ref value);
    }

   

    public override void BeginArray(Field field, ArrayType type)
    {
        // do nothing
    }

    public override void EndArray()
    {
        // do nothing
    }

    public override void BeginStruct(Field field, StructType type)
    {
        // do nothing
    }

    public override void EndStruct()
    {
        // do nothing
    }

    public override void BeginOptionalStruct(Field field, OptionalStructType type, bool isPresent, out bool createNew)
    {
        var temp = false;
        BinSerialize.ReadBool(ref Memory, ref temp);
        createNew = temp;
    }

    public override void EndOptionalStruct(bool isPresent)
    {
        // do nothing
    }

    public override void BeginList(Field field, ListType type, ref uint size)
    {
        BinSerialize.ReadUInt(ref Memory, ref size);
    }

    public override void EndList()
    {
        // do nothing
    }

}