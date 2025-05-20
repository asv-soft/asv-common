namespace Asv.IO;

public class SimpleBinarySizeCalculator(bool skipUnknown) : FullVisitorBase(skipUnknown)
{
    public int Size { get; private set; }

    public override void Visit(Field field, ref byte value)
    {
        Size += sizeof(byte);
    }

    public override void Visit(Field field, ref sbyte value)
    {
        Size += sizeof(sbyte);
    }

    public override void Visit(Field field, ref short value)
    {
        Size += sizeof(short);
    }

    public override void Visit(Field field, ref ushort value)
    {
        Size += sizeof(ushort);
    }

    public override void Visit(Field field, ref int value)
    {
        Size += sizeof(int);
    }

    public override void Visit(Field field, ref uint value)
    {
        Size += sizeof(uint);
    }

    public override void Visit(Field field, ref long value)
    {
        Size += sizeof(long);
    }

    public override void Visit(Field field, ref ulong value)
    {
        Size += sizeof(ulong);
    }

    public override void Visit(Field field, ref float value)
    {
        Size += sizeof(float);
    }

    public override void Visit(Field field, ref double value)
    {
        Size += sizeof(double);
    }

    public override void Visit(Field field, ref string value)
    {
        Size+= BinSerialize.GetSizeForString(value);
    }

    public override void Visit(Field field, ref bool value)
    {
        Size += sizeof(bool);
    }

    public override void Visit(Field field, ref char value)
    {
        Size += 8;
    }

    public override void BeginArray(Field field, int size)
    {
        // fixed size array => skip
    }

    public override void EndArray()
    {
        // fixed size array => skip
    }

    public override void BeginStruct(Field field)
    {
        // fixed size struct => skip
    }

    public override void EndStruct()
    {
        // fixed size struct => skip
    }

    public override void BeginList(Field field, ref uint size)
    {
        Size += sizeof(uint);
    }

    public override void EndList()
    {
        // do nothing
    }
}