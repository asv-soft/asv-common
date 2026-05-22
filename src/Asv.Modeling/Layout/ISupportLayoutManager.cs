namespace Asv.Modeling;

/// <summary>
/// Exposes a layout manager for an object tree.
/// </summary>
/// <typeparam name="TBase">The routed event and navigation base type.</typeparam>
public interface ISupportLayoutManager<TBase>
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
{
    /// <summary>
    /// Gets the layout manager for the object tree.
    /// </summary>
    ILayoutManager<TBase> LayoutManager { get; }
}
