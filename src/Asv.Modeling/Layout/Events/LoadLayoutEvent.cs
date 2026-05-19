namespace Asv.Modeling;

public class LoadLayoutEvent<TBase>(TBase sender, ILayoutData layoutData, string layoutId)
    : AsyncRoutedEvent<TBase>(sender, RoutingStrategy.Bubble)
    where TBase : ISupportRoutedEvents<TBase>
{
    public string LayoutId => layoutId;
    public ILayoutData LayoutData => layoutData;
    public bool IsLoaded { get; set; }
}
