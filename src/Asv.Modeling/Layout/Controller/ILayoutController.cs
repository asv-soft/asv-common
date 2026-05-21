namespace Asv.Modeling;

public interface ILayoutSink<in TData> : IDisposable
    where TData : ILayoutData
{
    void Load();

    ValueTask LoadAsync(CancellationToken cancel = default);

    void Save(TData data);
}

public delegate ValueTask LoadLayoutCallback<in TData>(
    TData data,
    CancellationToken cancel = default
);

public interface ILayoutController : IDisposable
{
    ILayoutSink<TData> Register<TData>(string layoutId, LoadLayoutCallback<TData> loadLayout)
        where TData : ILayoutData;

    void LoadAll();
}
