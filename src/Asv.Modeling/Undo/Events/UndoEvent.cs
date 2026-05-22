namespace Asv.Modeling;

/// <summary>
/// Routed event that publishes a model change into undo history.
/// </summary>
/// <typeparam name="TBase">The routed event node type.</typeparam>
/// <param name="sender">The node that published the undoable change.</param>
/// <param name="undoChange">The change payload.</param>
/// <param name="changeId">The change registration identifier.</param>
public class UndoEvent<TBase>(TBase sender, IUndoChange undoChange, string changeId)
    : AsyncRoutedEvent<TBase>(sender, RoutingStrategy.Bubble)
    where TBase : ISupportRoutedEvents<TBase>
{
    /// <summary>
    /// Gets the change registration identifier.
    /// </summary>
    public string ChangeId => changeId;

    /// <summary>
    /// Gets the published undo change payload.
    /// </summary>
    public IUndoChange UndoChange => undoChange;
}
