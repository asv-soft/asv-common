namespace Asv.IO;

public interface ISupportSchema
{
    Schema Schema { get; }
    void Serialize(ISerializeVisitor visitor);
    void Deserialize(IDeserializeVisitor visitor);
}

public interface ISerializeVisitor
{
    
}

public interface ISerializeVisitor<T>: ISerializeVisitor
{
    void Write(ref readonly T value);
}

public interface IDeserializeVisitor
{
    
}

public interface IDeserializeVisitor<T> : IDeserializeVisitor
{
    void Read(ref T value);
}



