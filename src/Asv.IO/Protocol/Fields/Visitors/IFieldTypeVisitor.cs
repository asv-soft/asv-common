namespace Asv.IO;

public interface IFieldTypeVisitor<in T>: IFieldTypeVisitor
    where T: IFieldType
{
    void Visit(T type);
}

public interface IFieldTypeVisitor
{
    void Visit(IFieldType type);
}