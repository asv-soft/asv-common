namespace Asv.Modeling;

/// <summary>
/// Handles layout load and save events for a navigation tree.
/// </summary>
/// <typeparam name="TBase">The routed event and navigation base type.</typeparam>
public interface ILayoutManager<TBase> : IDisposable
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
{
    /// <summary>
    /// Gets the store used to persist layout values.
    /// </summary>
    ILayoutStore Store { get; }
}
