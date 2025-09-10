namespace Asv.IO;

public interface IVisitor
{
    void VisitUnknown(Field field, IFieldType type);
}

public interface IReferenceVisitor<in TType, TValue> : IVisitor
{
    void Visit(Field field, TType type, ref TValue value);
}

public interface IFullVisitor
    : ListType.IVisitor,
        ArrayType.IVisitor,
        StructType.IVisitor,
        OptionalStructType.IVisitor,
        DoubleType.IVisitor,
        DoubleOptionalType.IVisitor,
        FloatType.IVisitor,
        FloatOptionalType.IVisitor,
        HalfFloatType.IVisitor,
        HalfFloatOptionalType.IVisitor,
        Int8Type.IVisitor,
        Int8OptionalType.IVisitor,
        Int16Type.IVisitor,
        Int16OptionalType.IVisitor,
        Int32Type.IVisitor,
        Int32OptionalType.IVisitor,
        Int64Type.IVisitor,
        Int64OptionalType.IVisitor,
        UInt8Type.IVisitor,
        UInt8OptionalType.IVisitor,
        UInt16Type.IVisitor,
        UInt16OptionalType.IVisitor,
        UInt32Type.IVisitor,
        UInt32OptionalType.IVisitor,
        UInt64Type.IVisitor,
        UInt64OptionalType.IVisitor,
        StringType.IVisitor,
        StringOptionalType.IVisitor,
        BoolType.IVisitor,
        BoolOptionalType.IVisitor,
        CharType.IVisitor,
        CharOptionalType.IVisitor,
        DateTimeType.IVisitor,
        DateTimeOptionalType.IVisitor,
        TimeSpanType.IVisitor,
        TimeSpanOptionalType.IVisitor,
        DateOnlyType.IVisitor,
        DateOnlyOptionalType.IVisitor,
        TimeOnlyType.IVisitor,
        TimeOnlyOptionalType.IVisitor;
