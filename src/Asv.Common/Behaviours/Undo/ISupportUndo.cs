namespace Asv.Common;

public interface ISupportUndo<TBase, TId> : ISupportRoutedEvents<TBase>, ISupportNavigation<TId, TBase>
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TId, TBase>
{
    IUndoController<TBase, TId> Undo { get; }
}
