namespace Asv.IO;

public interface IVisitor
{
    void VisitUnknown(Field field);
}

public interface IVisitor<TValue> : IVisitor
{
    void Visit(Field field, ref TValue value);
}

public interface IArrayVisitor: IVisitor
{
    void BeginArray(Field field, int size);
    void EndArray();
}

public interface IStructVisitor: IVisitor
{
    void BeginStruct(Field field);
    void EndStruct();
}

public interface IFullVisitor : 
    IArrayVisitor,
    IStructVisitor,
    IListVisitor,
    IVisitor<char>,
    IVisitor<byte>,
    IVisitor<sbyte>,
    IVisitor<short>,
    IVisitor<ushort>,
    IVisitor<int>,
    IVisitor<uint>,
    IVisitor<long>,
    IVisitor<ulong>,
    IVisitor<float>,
    IVisitor<double>,
    IVisitor<string>,
    IVisitor<bool>;