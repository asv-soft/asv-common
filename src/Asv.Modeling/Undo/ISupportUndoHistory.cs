namespace Asv.Modeling;

public interface ISupportUndoHistory<TBase> : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
{
    IUndoHistory<TBase> UndoHistory { get; }
}
