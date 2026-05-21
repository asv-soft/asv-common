namespace Asv.Modeling;

public abstract class LoadLayoutEvent<TBase>(TBase sender, string layoutId)
    : AsyncRoutedEvent<TBase>(sender, RoutingStrategy.Bubble)
    where TBase : ISupportRoutedEvents<TBase>
{
    public string LayoutId => layoutId;

    public abstract object UntypedLayoutData { get; }

    public bool IsLoaded { get; set; }

    internal abstract bool TryLoad(ILayoutStore store, NavPath path);
}

public sealed class LoadLayoutEvent<TBase, TData>(TBase sender, string layoutId)
    : LoadLayoutEvent<TBase>(sender, layoutId)
    where TBase : ISupportRoutedEvents<TBase>
{
    private TData _layoutData = default!;

    public TData LayoutData =>
        IsLoaded
            ? _layoutData
            : throw new InvalidOperationException("Layout data has not been loaded yet.");

    public override object UntypedLayoutData => LayoutData!;

    internal override bool TryLoad(ILayoutStore store, NavPath path)
    {
        if (store.TryLoad<TData>(path, LayoutId, out var layoutData) == false)
        {
            return false;
        }

        _layoutData = layoutData;
        IsLoaded = true;
        return true;
    }
}
