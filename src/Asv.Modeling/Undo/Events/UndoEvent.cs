namespace Asv.Modeling;

public class UndoEvent<TBase>(TBase sender, IUndoChange undoChange, string changeId)
    : AsyncRoutedEvent<TBase>(sender, RoutingStrategy.Bubble)
    where TBase : ISupportRoutedEvents<TBase>
{
    public string ChangeId => changeId;
    public IUndoChange UndoChange => undoChange;
}
