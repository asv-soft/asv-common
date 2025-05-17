namespace Asv.IO;

public struct SimpleBinarySizeCalculator(bool skipUnknown) : IFullVisitor
{
    public int Size { get; private set; }

    public void Visit(Field field, ref byte value)
    {
        Size += sizeof(byte);
    }

    public void Visit(Field field, ref sbyte value)
    {
        Size += sizeof(sbyte);
    }

    public void Visit(Field field, ref short value)
    {
        Size += sizeof(short);
    }

    public void Visit(Field field, ref ushort value)
    {
        Size += sizeof(ushort);
    }

    public void Visit(Field field, ref int value)
    {
        Size += sizeof(int);
    }

    public void Visit(Field field, ref uint value)
    {
        Size += sizeof(uint);
    }

    public void Visit(Field field, ref long value)
    {
        Size += sizeof(long);
    }

    public void Visit(Field field, ref ulong value)
    {
        Size += sizeof(ulong);
    }

    public void Visit(Field field, ref float value)
    {
        Size += sizeof(float);
    }

    public void Visit(Field field, ref double value)
    {
        Size += sizeof(double);
    }

    public void Visit(Field field, ref string value)
    {
        Size+= BinSerialize.GetSizeForString(value);
    }

    public void Visit(Field field, ref bool value)
    {
        Size += sizeof(bool);
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
        Size += sizeof(uint);
    }

    public void EndList()
    {
        // do nothing
    }
}