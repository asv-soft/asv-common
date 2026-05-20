namespace Asv.Modeling;

public abstract class SaveLayoutEvent<TBase>(TBase sender, string layoutId)
    : AsyncRoutedEvent<TBase>(sender, RoutingStrategy.Bubble)
    where TBase : ISupportRoutedEvents<TBase>
{
    public string LayoutId => layoutId;
    public abstract ILayoutData UntypedLayoutData { get; }

    internal abstract void Save(ILayoutStore store, NavPath path);
}

public sealed class SaveLayoutEvent<TBase, TData>(TBase sender, TData layoutData, string layoutId)
    : SaveLayoutEvent<TBase>(sender, layoutId)
    where TBase : ISupportRoutedEvents<TBase>
    where TData : IJsonLayoutData<TData>
{
    public TData LayoutData => layoutData;

    public override ILayoutData UntypedLayoutData => layoutData;

    internal override void Save(ILayoutStore store, NavPath path)
    {
        store.Save(path, LayoutId, layoutData);
    }
}
