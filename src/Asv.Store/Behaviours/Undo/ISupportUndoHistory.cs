namespace Asv.Common;

public interface ISupportUndoHistory<TBase, TId>
    : ISupportRoutedEvents<TBase>,
        ISupportNavigation<TBase, TId>
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase, TId>
{
    IUndoHistory<TBase, TId> UndoHistory { get; }
}
