using ObservableCollections;
using R3;

namespace Asv.Modeling;

public interface IUndoHistory<TBase> : IDisposable
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
{
    ReactiveCommand Undo { get; }
    ValueTask UndoAsync(CancellationToken cancel = default);
    IObservableCollection<IUndoSnapshot> UndoStack { get; }
    ReactiveCommand Redo { get; }
    ValueTask RedoAsync(CancellationToken cancel = default);
    IObservableCollection<IUndoSnapshot> RedoStack { get; }
}