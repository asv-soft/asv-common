namespace Asv.IO;

public abstract class FullVisitorBase(bool skipUnknown)  : IFullVisitor
{
    public abstract void Visit(Field field, ref char value);

    public abstract void Visit(Field field, ref byte value);

    public abstract void Visit(Field field, ref sbyte value);

    public abstract void Visit(Field field, ref short value);

    public abstract void Visit(Field field, ref ushort value);

    public abstract void Visit(Field field, ref int value);

    public abstract void Visit(Field field, ref uint value);

    public abstract void Visit(Field field, ref long value);

    public abstract void Visit(Field field, ref ulong value);

    public abstract void Visit(Field field, ref float value);

    public abstract void Visit(Field field, ref double value);

    public abstract void Visit(Field field, ref string value);

    public abstract void Visit(Field field, ref bool value);

    public void VisitUnknown(Field field)
    {
        if (skipUnknown)
        {
            return;
        }

        throw new System.NotImplementedException($"Unknown field {field.Name} [{field}]");
    }

    public abstract void BeginArray(Field field, int size);
    public abstract void EndArray();
    public abstract void BeginStruct(Field field);
    public abstract void EndStruct();
    public abstract void BeginList(Field field, ref uint size);
    public abstract void EndList();
}