namespace Asv.Modeling;

public sealed class NullLayoutStore : ILayoutStore
{
    public static ILayoutStore Instance { get; } = new NullLayoutStore();

    private NullLayoutStore() { }

    public bool TryLoad<TData>(NavPath path, string layoutId, out TData layoutData)
        where TData : ILayoutData, new()
    {
        layoutData = default!;
        return false;
    }

    public void Save<TData>(NavPath path, string layoutId, TData layoutData)
        where TData : ILayoutData { }

    public void Dispose() { }
}
