namespace Asv.Store;

public interface IMetadataPart<T>
{
    void Write(T? metadata);
    T? Read();
}
