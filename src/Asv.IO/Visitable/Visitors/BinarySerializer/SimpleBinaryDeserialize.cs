using System;
using System.Buffers;

namespace Asv.IO;

public delegate void ReadDelegate(ref Span<byte> buffer);

public class SimpleBinaryDeserialize(ReadOnlyMemory<byte> memory, bool skipUnknown) : FullVisitorBase(skipUnknown)
{
    public ReadOnlyMemory<byte> Memory = memory;

    public override void Visit(Field field, ref sbyte value)
    {
        BinSerialize.ReadSByte(ref Memory, ref value);
    }

    public override void Visit(Field field, ref short value)
    {
        BinSerialize.ReadShort(ref Memory, ref value);        
    }

    public override void Visit(Field field, ref ushort value)
    {
        BinSerialize.ReadUShort(ref Memory, ref value);
    }

    public override void Visit(Field field, ref int value)
    {
        BinSerialize.ReadInt(ref Memory, ref value);
    }

    public override void Visit(Field field, ref uint value)
    {
        BinSerialize.ReadUInt(ref Memory, ref value);        
    }

    public override void Visit(Field field, ref long value)
    {
        BinSerialize.ReadLong(ref Memory, ref value);
    }

    public override void Visit(Field field, ref ulong value)
    {
        BinSerialize.ReadULong(ref Memory, ref value);
    }

    public override void Visit(Field field, ref float value)
    {
        BinSerialize.ReadFloat(ref Memory, ref value);
    }

    public override void Visit(Field field, ref double value)
    {
        BinSerialize.ReadDouble(ref Memory, ref value);
    }

    public override void Visit(Field field, ref string value)
    {
        value = BinSerialize.ReadString(ref Memory);
    }

    public override void Visit(Field field, ref bool value)
    {
        BinSerialize.ReadBool(ref Memory, ref value);
    }

    public override void Visit(Field field, ref char value)
    {
        var tmp = default(byte);
        BinSerialize.ReadByte(ref Memory, ref tmp);
        value = (char)tmp;
    }

    public override void Visit(Field field, ref byte value)
    {
        BinSerialize.ReadByte(ref Memory, ref value);
    }

   

    public override void BeginArray(Field field, int size)
    {
        // do nothing
    }

    public override void EndArray()
    {
        // do nothing
    }

    public override void BeginStruct(Field field)
    {
        // do nothing
    }

    public override void EndStruct()
    {
        // do nothing
    }

    public override void BeginList(Field field, IFieldType type, ref uint size)
    {
        BinSerialize.ReadUInt(ref Memory, ref size);
    }

    public override void EndList()
    {
        // do nothing
    }

}