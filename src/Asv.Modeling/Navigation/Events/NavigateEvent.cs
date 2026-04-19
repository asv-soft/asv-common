namespace Asv.Modeling;

public class NavigateEvent<TBase>(TBase sender, NavPath path)
    : AsyncRoutedEvent<TBase>(sender, RoutingStrategy.Bubble)
    where TBase : ISupportRoutedEvents<TBase>
{
    public NavPath Path => path;
}