using System;

namespace Asv.IO;

public abstract class FullVisitorBase(bool skipUnknown)  : IFullVisitor
{
    public abstract void Visit(Field field, DoubleOptionalType type, ref double? value);

    public abstract void Visit(Field field, FloatOptionalType type, ref float? value);

    public abstract void Visit(Field field, HalfFloatOptionalType type, ref Half? value);

    public abstract void Visit(Field field, Int8OptionalType type, ref sbyte? value);

    public abstract void Visit(Field field, Int16OptionalType type, ref short? value);

    public abstract void Visit(Field field, Int32OptionalType type, ref int? value);

    public abstract void Visit(Field field, Int64OptionalType type, ref long? value);

    public abstract void Visit(Field field, UInt8OptionalType type, ref byte? value);

    public abstract void Visit(Field field, UInt16OptionalType type, ref ushort? value);

    public abstract void Visit(Field field, UInt32OptionalType type, ref uint? value);

    public abstract void Visit(Field field, UInt64OptionalType type, ref ulong? value);

    public abstract void Visit(Field field, StringOptionalType type, ref string? value);

    public abstract void Visit(Field field, BoolOptionalType type, ref bool? value);

    public abstract void Visit(Field field, CharOptionalType type, ref char? value);

    public abstract void Visit(Field field, DateTimeType type, ref DateTime value);

    public abstract void Visit(Field field, DateTimeOptionalType type, ref DateTime? value);

    public abstract void Visit(Field field, TimeSpanType type, ref TimeSpan value);

    public abstract void Visit(Field field, TimeSpanOptionalType type, ref TimeSpan? value);

    public abstract void Visit(Field field, DateOnlyType type, ref DateOnly value);

    public abstract void Visit(Field field, DateOnlyOptionalType type, ref DateOnly? value);

    public abstract void Visit(Field field, TimeOnlyType type, ref TimeOnly value);

    public abstract void Visit(Field field, TimeOnlyOptionalType type, ref TimeOnly? value);

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
    public abstract void BeginArray(Field field, ArrayType fieldType);
    public abstract void EndArray();
    public abstract void BeginStruct(Field field, StructType type);
    public abstract void EndStruct();
    public abstract void BeginOptionalStruct(Field field, OptionalStructType type, bool isPresent, out bool createNew);
    public abstract void EndOptionalStruct(bool isPresent);
}