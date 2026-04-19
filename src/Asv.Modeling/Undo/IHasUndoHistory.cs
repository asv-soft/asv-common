namespace Asv.Modeling;

public interface IHasUndoHistory<TBase>
    : ISupportRoutedEvents<TBase>,
        ISupportNavigation<TBase>
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
{
    IUndoHistory<TBase> UndoHistory { get; }
}
