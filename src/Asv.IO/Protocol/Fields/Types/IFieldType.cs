namespace Asv.IO;

public enum FieldTypeId
{
    Null,
    Boolean,
    UInt8,
    Int8,
    UInt16,
    Int16,
    UInt32,
    Int32,
    UInt64,
    Int64,
    HalfFloat,
    Float,
    Double,
    String,
    Array,
    Schema
}

public interface IFieldType
{
    FieldTypeId TypeId { get; }

    string Name { get; }
 
    void Accept(IFieldTypeVisitor visitor);

    bool IsFixedWidth { get; }
}