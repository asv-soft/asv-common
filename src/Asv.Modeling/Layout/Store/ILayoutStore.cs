namespace Asv.Modeling;

public interface ILayoutStore : IDisposable
{
    bool TryLoad<TData>(NavPath path, string layoutId, out TData layoutData)
        where TData : IJsonLayoutData<TData>;

    void Save<TData>(NavPath path, string layoutId, TData layoutData)
        where TData : IJsonLayoutData<TData>;
}
