namespace Asv.Modeling;

public interface ISupportUndo<TBase>
    : ISupportRoutedEvents<TBase>,
        ISupportNavigation<TBase>
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
{
    IUndoController Undo { get; }
}
