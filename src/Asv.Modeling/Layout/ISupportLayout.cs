namespace Asv.Modeling;

/// <summary>
/// Marks an object as supporting layout registration.
/// </summary>
public interface ISupportLayout
{
    /// <summary>
    /// Gets the layout controller used to register loadable and saveable layout values.
    /// </summary>
    ILayoutController Layout { get; }
}
