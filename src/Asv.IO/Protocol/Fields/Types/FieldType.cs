namespace Asv.IO;

public abstract class FieldType: IFieldType
{
    public abstract FieldTypeId TypeId { get; }

    public abstract string Name { get; }

    public virtual bool IsFixedWidth => false;

    public abstract void Accept(IFieldTypeVisitor visitor);

    internal static void Accept<T>(T type, IFieldTypeVisitor visitor)
        where T: class, IFieldType
    {
        switch (visitor)
        {
            case IFieldTypeVisitor<T> typedVisitor:
                typedVisitor.Visit(type);
                break;
            default:
                visitor.Visit(type);
                break;
        }
    }
}