namespace Asv.IO;

public interface IFieldType
{
    string Name { get; }
}

public abstract class FieldType: IFieldType
{
    public abstract string Name { get; }
}

public abstract class FieldType<TSelf, TValue> : FieldType
    where TSelf : IFieldType
{
    public static void Accept(Asv.IO.IVisitor visitor, Field field, IFieldType type, ref TValue value)
    {
        if (visitor is IVisitor accept)
        {
            accept.Visit(field, (TSelf)type, ref value);
        }
        else
        {
            visitor.VisitUnknown(field, type);
        }
    }
    public static void Accept(Asv.IO.IVisitor visitor, Field field, ref TValue value)
    {
        Accept(visitor, field, field.DataType, ref value);
    }
    public interface IVisitor : IReferenceVisitor<TSelf, TValue>;
}


