using System;

namespace Asv.IO;

public ref struct SimpleBinarySerialize(Span<byte> buffer, bool skipUnknown) : IFullVisitor
{
    private Span<byte> _buffer = buffer;

    public Span<byte> Buffer => _buffer;

    public void Visit(Field field, ref byte value)
    {
        BinSerialize.WriteByte(ref _buffer, value);
    }

    public void Visit(Field field, ref sbyte value)
    {
        BinSerialize.WriteSByte(ref _buffer, value);
    }

    public void Visit(Field field, ref short value)
    {
        BinSerialize.WriteShort(ref _buffer, value);
    }

    public void Visit(Field field, ref ushort value)
    {
        BinSerialize.WriteUShort(ref _buffer, value);
    }

    public void Visit(Field field, ref int value)
    {
        BinSerialize.WriteInt(ref _buffer, value);
    }

    public void Visit(Field field, ref uint value)
    {
        BinSerialize.WriteUInt(ref _buffer, value);
    }

    public void Visit(Field field, ref long value)
    {
        BinSerialize.WriteLong(ref _buffer, value);
    }

    public void Visit(Field field, ref ulong value)
    {
        BinSerialize.WriteULong(ref _buffer, value);
    }

    public void Visit(Field field, ref float value)
    {
        BinSerialize.WriteFloat(ref _buffer, value);
    }

    public void Visit(Field field, ref double value)
    {
        BinSerialize.WriteDouble(ref _buffer, value);
    }

    public void Visit(Field field, ref string value)
    {
        BinSerialize.WriteString(ref _buffer, value);
    }

    public void Visit(Field field, ref bool value)
    {
        BinSerialize.WriteBool(ref _buffer, value);
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
        // fixed size array => skip
    }

    public void EndArray()
    {
        // fixed size array => skip
    }

    public void BeginStruct(Field field)
    {
        // fixed size struct => skip
    }

    public void EndStruct()
    {
        // fixed size struct => skip
    }

    public void BeginList(Field field, ref uint size)
    {
        BinSerialize.WriteUInt(ref _buffer, size);
    }

    public void EndList()
    {
        
    }
}