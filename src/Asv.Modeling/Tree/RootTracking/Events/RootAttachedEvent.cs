namespace Asv.Modeling;

public class RootAttachedEvent<TBase, TRoot>(TBase sender, TRoot root)
    : AsyncRoutedEvent<TBase>(sender, RoutingStrategy.Tunnel) where TBase : ISupportRoutedEvents<TBase>
{
    public TRoot Root => root;
}