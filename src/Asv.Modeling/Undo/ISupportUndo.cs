namespace Asv.Modeling;

/// <summary>
/// Exposes an undo controller for a routed and navigable tree node.
/// </summary>
/// <typeparam name="TBase">The tree node type.</typeparam>
public interface ISupportUndo<TBase> : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
{
    /// <summary>
    /// Gets the undo controller used to register and publish undoable changes.
    /// </summary>
    IUndoController Undo { get; }
}
