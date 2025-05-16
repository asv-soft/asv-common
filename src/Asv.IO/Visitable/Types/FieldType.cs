namespace Asv.IO;

public abstract class FieldType: IFieldType
{
    public abstract FieldTypeId TypeId { get; }

    public abstract string Name { get; }

    public virtual bool IsFixedWidth => false;
    
    protected static void Accept<TValue>(IVisitor visitor, Field field, ref TValue value)
    {
        if (visitor is IVisitor<TValue> accept)
        {
            accept.Visit(field, ref value);
        }
        else
        {
            visitor.VisitUnknown(field);
        }
        
    }
}