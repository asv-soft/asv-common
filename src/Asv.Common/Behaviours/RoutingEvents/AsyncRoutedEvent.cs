namespace Asv.Common;

public enum RoutingStrategy
{
    Bubble,
    Tunnel,
    Direct,
}

public abstract class AsyncRoutedEvent<T>(T sender, RoutingStrategy strategy)
    where T : ISupportRoutedEvents<T>
{
    public RoutingStrategy Strategy => strategy;
    public T Sender => sender;
    public bool IsHandled { get; set; }
}
