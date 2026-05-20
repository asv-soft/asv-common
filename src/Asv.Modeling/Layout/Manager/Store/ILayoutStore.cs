namespace Asv.Modeling;

public interface ILayoutStore : IDisposable
{
    bool TryLoad<TData>(NavPath path, string layoutId, out TData layoutData)
        where TData : ILayoutData, new();

    void Save<TData>(NavPath path, string layoutId, TData layoutData)
        where TData : ILayoutData;
}
