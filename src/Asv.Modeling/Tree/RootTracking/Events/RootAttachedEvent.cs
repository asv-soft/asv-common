namespace Asv.Modeling;

/// <summary>
/// Routed event raised when a tree node becomes attached to a root.
/// </summary>
/// <typeparam name="TBase">The routed event node type.</typeparam>
/// <typeparam name="TRoot">The attached root type.</typeparam>
/// <param name="sender">The node that raised the event.</param>
/// <param name="root">The root that the node is attached to.</param>
public class RootAttachedEvent<TBase, TRoot>(TBase sender, TRoot root)
    : AsyncRoutedEvent<TBase>(sender, RoutingStrategy.Tunnel)
    where TBase : ISupportRoutedEvents<TBase>
{
    /// <summary>
    /// Gets the root that the sender is attached to.
    /// </summary>
    public TRoot Root => root;
}
