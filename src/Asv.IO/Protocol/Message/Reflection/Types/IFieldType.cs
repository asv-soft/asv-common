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
    Struct
}

public interface IFieldType
{
    FieldTypeId TypeId { get; }

    string Name { get; }
 
    void Accept(IFieldTypeVisitor visitor);

    bool IsFixedWidth { get; }
}

public interface IFieldTypeVisitor
{
    void Visit(IFieldType type);
}

public interface IFieldTypeVisitor<in T>: IFieldTypeVisitor
    where T: IFieldType
{
    void Visit(T type);
}

