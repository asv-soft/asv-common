namespace Asv.Modeling;

public interface ILayoutSink<in TData> : IDisposable
    where TData : ILayoutData
{
    void Load();

    ValueTask LoadAsync(CancellationToken cancel = default);

    void Save(TData data);
}

public interface ILayoutController : IDisposable
{
    ILayoutSink<TData> Register<TData>(string layoutId, Action<TData> load)
        where TData : ILayoutData, new();

    void LoadAll();
}
