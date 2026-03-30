namespace Asv.IO;

public interface IChimpVisitor
    : DoubleType.IVisitor,
        FloatType.IVisitor,
        Int8Type.IVisitor,
        Int16Type.IVisitor,
        Int32Type.IVisitor,
        Int64Type.IVisitor,
        UInt8Type.IVisitor,
        UInt16Type.IVisitor,
        UInt32Type.IVisitor,
        UInt64Type.IVisitor,
        CharType.IVisitor,
        ArrayType.IVisitor,
        StructType.IVisitor,
        BoolType.IVisitor { }
