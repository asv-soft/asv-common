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
    List,
    Schema,
    Struct,
    Time32,
    Time64,
    AsciiChar
}

public interface IFieldType
{
    FieldTypeId TypeId { get; }

    string Name { get; }
 
    bool IsFixedWidth { get; }
}
