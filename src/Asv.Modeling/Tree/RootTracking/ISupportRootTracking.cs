namespace Asv.Modeling;

/// <summary>
/// Exposes root tracking for an object in a tree.
/// </summary>
/// <typeparam name="TBase">The tree node type.</typeparam>
/// <typeparam name="TRoot">The root node type tracked by the object.</typeparam>
public interface ISupportRootTracking<TBase, TRoot>
    where TRoot : TBase
{
    /// <summary>
    /// Gets the root tracking controller for this object.
    /// </summary>
    IRootTrackingController<TRoot> RootTracking { get; }
}
