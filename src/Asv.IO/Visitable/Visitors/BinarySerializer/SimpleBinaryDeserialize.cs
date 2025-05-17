using System;

namespace Asv.IO;

public ref struct SimpleBinaryDeserialize(ReadOnlySpan<byte> buffer, bool skipUnknown) : IFullVisitor
{
    private ReadOnlySpan<byte> _buffer = buffer;

    public ReadOnlySpan<byte> Buffer => _buffer;
    
    public void Visit(Field field, ref byte value)
    {
        BinSerialize.ReadByte(ref _buffer, ref value);
    }

    public void Visit(Field field, ref sbyte value)
    {
        BinSerialize.ReadSByte(ref _buffer, ref value);
    }

    public void Visit(Field field, ref short value)
    {
        BinSerialize.ReadShort(ref _buffer, ref value);
    }

    public void Visit(Field field, ref ushort value)
    {
        BinSerialize.ReadUShort(ref _buffer, ref value);
    }

    public void Visit(Field field, ref int value)
    {
        BinSerialize.ReadInt(ref _buffer, ref value);
    }

    public void Visit(Field field, ref uint value)
    {
        BinSerialize.ReadUInt(ref _buffer, ref value);
    }

    public void Visit(Field field, ref long value)
    {
        BinSerialize.ReadLong(ref _buffer, ref value);
    }

    public void Visit(Field field, ref ulong value)
    {
        BinSerialize.ReadULong(ref _buffer, ref value);
    }

    public void Visit(Field field, ref float value)
    {
        BinSerialize.ReadFloat(ref _buffer, ref value);
    }

    public void Visit(Field field, ref double value)
    {
        BinSerialize.ReadDouble(ref _buffer, ref value);
    }

    public void Visit(Field field, ref string value)
    {
        value = BinSerialize.ReadString(ref _buffer);
    }

    public void Visit(Field field, ref bool value)
    {
        BinSerialize.ReadBool(ref _buffer, ref value);
    }

    public void VisitUnknown(Field field)
    {
        if (skipUnknown)
        {
            return;
        }

        throw new System.NotImplementedException($"Unknown field {field.Name} [{field}]");
    }

    public void BeginArray(Field field, int size)
    {
        // do nothing
    }

    public void EndArray()
    {
        // do nothing
    }

    public void BeginStruct(Field field)
    {
        // do nothing
    }

    public void EndStruct()
    {
        // do nothing
    }

    public void BeginList(Field field, ref uint size)
    {
        BinSerialize.ReadUInt(ref _buffer, ref size);
    }

    public void EndList()
    {
        // do nothing
    }
}