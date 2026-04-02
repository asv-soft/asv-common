namespace Asv.Common;

public class UndoEvent<TBase>(TBase sender, IChange change, string changeId)
    : AsyncRoutedEvent<TBase>(sender, RoutingStrategy.Bubble)
    where TBase : ISupportRoutedEvents<TBase>
{
    public string ChangeId => changeId;
    public IChange Change => change;
}
