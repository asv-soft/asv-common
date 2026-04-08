namespace Asv.Modeling;

public interface ISupportUndo<TBase, TId>
    : ISupportRoutedEvents<TBase>,
        ISupportNavigation<TBase, TId>
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase, TId>
{
    IUndoController Undo { get; }
}
