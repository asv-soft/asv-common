namespace Asv.Modeling;

public class RootDetachedEvent<TBase>(TBase sender)
    : AsyncRoutedEvent<TBase>(sender, RoutingStrategy.Tunnel) where TBase : ISupportRoutedEvents<TBase>
{
    
}