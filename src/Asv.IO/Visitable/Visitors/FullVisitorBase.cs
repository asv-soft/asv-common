using System;

namespace Asv.IO;

public abstract class FullVisitorBase(bool skipUnknown)  : IFullVisitor
{
    public virtual void VisitUnknown(Field field, IFieldType type)
    {
        if (skipUnknown)
        {
            return;
        }

        throw new NotImplementedException($"Unknown field type {field.Name} [{field}]");
    }

    public abstract void Visit(Field field, DoubleType type, ref double value);
    public abstract void Visit(Field field, FloatType type, ref float value);
    public abstract void Visit(Field field, HalfFloatType type, ref Half value);
    public abstract void Visit(Field field, Int8Type type, ref sbyte value);
    public abstract void Visit(Field field, Int16Type type, ref short value);
    public abstract void Visit(Field field, Int32Type type, ref int value);
    public abstract void Visit(Field field, Int64Type type, ref long value);
    public abstract void Visit(Field field, UInt8Type type, ref byte value);
    public abstract void Visit(Field field, UInt16Type type, ref ushort value);
    public abstract void Visit(Field field, UInt32Type type, ref uint value);
    public abstract void Visit(Field field, UInt64Type type, ref ulong value);
    public abstract void Visit(Field field, StringType type, ref string value);
    public abstract void Visit(Field field, BoolType type, ref bool value);
    public abstract void Visit(Field field, CharType type, ref char value);
    public abstract void BeginList(Field field, ListType type, ref uint size);
    public abstract void EndList();
    public abstract void BeginArray(Field field, ArrayType fieldType, int size);
    public abstract void EndArray();
    public abstract void BeginStruct(Field field, StructType type);
    public abstract void EndStruct();
}