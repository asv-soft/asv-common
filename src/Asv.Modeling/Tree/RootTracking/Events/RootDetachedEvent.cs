namespace Asv.Modeling;

/// <summary>
/// Routed event raised when a tree node becomes detached from its root.
/// </summary>
/// <typeparam name="TBase">The routed event node type.</typeparam>
/// <param name="sender">The node that raised the event.</param>
public class RootDetachedEvent<TBase>(TBase sender)
    : AsyncRoutedEvent<TBase>(sender, RoutingStrategy.Tunnel)
    where TBase : ISupportRoutedEvents<TBase> { }
