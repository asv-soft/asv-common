namespace Asv.Modeling;

/// <summary>
/// Exposes undo history for a routed and navigable tree node.
/// </summary>
/// <typeparam name="TBase">The tree node type.</typeparam>
public interface ISupportUndoHistory<TBase> : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
{
    /// <summary>
    /// Gets the undo history used to execute undo and redo operations.
    /// </summary>
    IUndoHistory<TBase> UndoHistory { get; }
}
