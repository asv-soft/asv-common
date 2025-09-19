using System;

namespace Asv.IO;

public abstract class ChimpVisitorBase : IChimpVisitor
{
    public abstract void Visit(Field field, DoubleType type, ref double value);
    public abstract void Visit(Field field, FloatType type, ref float value);
    public abstract void Visit(Field field, Int8Type type, ref sbyte value);
    public abstract void Visit(Field field, Int16Type type, ref short value);
    public abstract void Visit(Field field, Int32Type type, ref int value);
    public abstract void Visit(Field field, Int64Type type, ref long value);
    public abstract void Visit(Field field, UInt8Type type, ref byte value);
    public abstract void Visit(Field field, UInt16Type type, ref ushort value);
    public abstract void Visit(Field field, UInt32Type type, ref uint value);
    public abstract void Visit(Field field, UInt64Type type, ref ulong value);
    public abstract void Visit(Field field, CharType type, ref char value);
    public abstract void Visit(Field field, BoolType type, ref bool value);

    public void VisitUnknown(Field field, IFieldType type)
    {
        throw new NotSupportedException($"Unsupported filed '{field.Name}' type '{type.Name}'");
    }

    public void BeginArray(Field field, ArrayType fieldType)
    {
        // nothing to do
    }

    public void EndArray()
    {
        // nothing to do
    }

    public void BeginStruct(Field field, StructType type)
    {
        // nothing to do
    }

    public void EndStruct()
    {
        // nothing to do
    }
}
