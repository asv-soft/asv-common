namespace Asv.IO;

public interface IFixedType : IFieldType
{
    
}

public abstract class FixedType<TSelf, TValue> : FieldType<TSelf, TValue>, IFixedType
    where TSelf : IFieldType
{
    
}