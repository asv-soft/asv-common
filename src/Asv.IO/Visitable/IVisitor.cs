namespace Asv.IO;

public interface IVisitor
{
    void VisitUnknown(Field field, IFieldType type);
}

public interface IReferenceVisitor<in TType, TValue> : IVisitor
{
    void Visit(Field field, TType type, ref TValue value);
}

public interface IFullVisitor :
    ListType.IVisitor,
    ArrayType.IVisitor,
    StructType.IVisitor,
    DoubleType.IVisitor,
    FloatType.IVisitor,
    HalfFloatType.IVisitor,
    Int8Type.IVisitor,
    Int16Type.IVisitor,
    Int32Type.IVisitor,
    Int64Type.IVisitor,
    UInt8Type.IVisitor,
    UInt16Type.IVisitor,
    UInt32Type.IVisitor,
    UInt64Type.IVisitor,
    StringType.IVisitor,
    BoolType.IVisitor,
    CharType.IVisitor;
