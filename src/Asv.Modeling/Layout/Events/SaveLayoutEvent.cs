namespace Asv.Modeling;

public class SaveLayoutEvent<TBase>(TBase sender, ILayoutData layoutData, string layoutId)
    : AsyncRoutedEvent<TBase>(sender, RoutingStrategy.Bubble)
    where TBase : ISupportRoutedEvents<TBase>
{
    public string LayoutId => layoutId;
    public ILayoutData LayoutData => layoutData;
}
