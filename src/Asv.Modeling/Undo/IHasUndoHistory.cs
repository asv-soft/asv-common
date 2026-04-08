namespace Asv.Modeling;

public interface IHasUndoHistory<TBase, TId>
    : ISupportRoutedEvents<TBase>,
        ISupportNavigation<TBase, TId>
    where TBase : ISupportUndo<TBase, TId>
{
    IUndoHistory<TBase, TId> UndoHistory { get; }
}
