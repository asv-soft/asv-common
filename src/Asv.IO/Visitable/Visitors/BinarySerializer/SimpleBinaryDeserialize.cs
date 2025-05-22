using System;
using System.Buffers;

namespace Asv.IO;

public delegate void ReadDelegate(ref Span<byte> buffer);

public class SimpleBinaryDeserialize(ReadOnlyMemory<byte> memory, bool skipUnknown) : FullVisitorBase(skipUnknown)
{
    public ReadOnlyMemory<byte> Memory = memory;

    public override void Visit(Field field, HalfFloatType type, ref Half value)
    {
        BinSerialize.ReadHalf(ref Memory, ref value);
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

   

    public override void BeginArray(Field field, ArrayType type, int size)
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

    public override void BeginList(Field field, ListType type, ref uint size)
    {
        BinSerialize.ReadUInt(ref Memory, ref size);
    }

    public override void EndList()
    {
        // do nothing
    }

}