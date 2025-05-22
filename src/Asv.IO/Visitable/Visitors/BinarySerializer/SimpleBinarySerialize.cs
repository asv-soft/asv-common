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

    public override void BeginArray(Field field, ArrayType type, int size)
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

    public override void BeginList(Field field, ListType type, ref uint size)
    {
        BinSerialize.WriteUInt(buffer, size);
    }

    public override void EndList()
    {
        
    }
}