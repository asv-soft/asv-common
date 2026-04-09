namespace Asv.Modeling;

public interface IHasUndoHistory<TBase, TId>
    : ISupportRoutedEvents<TBase>,
        ISupportNavigation<TBase, TId>
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase, TId>
{
    IUndoHistory<TBase, TId> UndoHistory { get; }
}
