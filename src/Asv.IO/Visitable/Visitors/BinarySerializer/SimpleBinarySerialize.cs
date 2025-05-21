using System.Buffers;

namespace Asv.IO;

public readonly struct SimpleBinarySerialize(IBufferWriter<byte> buffer, bool skipUnknown) : IFullVisitor
{
    public void Visit(Field field, ref byte value)
    {
       BinSerialize.WriteByte(buffer, value);
    }

    public void Visit(Field field, ref sbyte value)
    {
        BinSerialize.WriteSByte(buffer, value);
    }

    public void Visit(Field field, ref short value)
    {
        BinSerialize.WriteShort(buffer, value);
    }

    public void Visit(Field field, ref ushort value)
    {
        BinSerialize.WriteUShort(buffer, value);
    }

    public void Visit(Field field, ref int value)
    {
        BinSerialize.WriteInt(buffer, value);
    }

    public void Visit(Field field, ref uint value)
    {
        BinSerialize.WriteUInt(buffer, value);
    }

    public void Visit(Field field, ref long value)
    {
        BinSerialize.WriteLong(buffer, value);
    }

    public void Visit(Field field, ref ulong value)
    {
        BinSerialize.WriteULong(buffer, value);
    }

    public void Visit(Field field, ref float value)
    {
        BinSerialize.WriteFloat(buffer, value);
    }

    public void Visit(Field field, ref double value)
    {
        BinSerialize.WriteDouble(buffer, value);
    }

    public void Visit(Field field, ref string value)
    {
        BinSerialize.WriteString(buffer, value);
    }

    public void Visit(Field field, ref bool value)
    {
        BinSerialize.WriteBool(buffer, value);
    }

    public void Visit(Field field, ref char value)
    {
        BinSerialize.WriteByte(buffer, (byte)value);
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

    public void BeginList(Field field, IFieldType type, ref uint size)
    {
        BinSerialize.WriteUInt(buffer, size);
    }

    public void EndList()
    {
        
    }
}