namespace Asv.IO;

public delegate void ElementDelegate(int index, IVisitor visitor, Field field, IFieldType fieldType);
public interface INestedWithSameType : IFieldType
{
    IFieldType ElementType { get; }
}