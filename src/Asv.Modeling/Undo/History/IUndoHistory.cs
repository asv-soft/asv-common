using Asv.Common;
using ObservableCollections;
using R3;

namespace Asv.Modeling;

public interface IUndoHistory<TBase, TId> : IDisposable
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase, TId>
{
    ReactiveCommand Undo { get; }
    ValueTask UndoAsync(CancellationToken cancel = default);
    IObservableCollection<IUndoSnapshot<TId>> UndoStack { get; }
    ReactiveCommand Redo { get; }
    ValueTask RedoAsync(CancellationToken cancel = default);
    IObservableCollection<IUndoSnapshot<TId>> RedoStack { get; }
}

public class UndoHistory<TBase, TId> : AsyncDisposableOnceBag, IUndoHistory<TBase, TId>
    where TBase : ISupportRoutedEvents<TBase>,
        ISupportNavigation<TBase, TId>,
        ISupportUndo<TBase, TId>
{
    private readonly TBase _owner;
    private readonly IUndoHistoryStore<TId> _store;
    private readonly ObservableStack<IUndoSnapshot<TId>> _undoStack = new();
    private readonly ObservableStack<IUndoSnapshot<TId>> _redoStack = new();

    public UndoHistory(TBase owner, IUndoHistoryStore<TId> store)
    {
        _owner = owner;
        _store = store.AddTo(ref DisposableBag);
        Undo = new ReactiveCommand((_, token) => UndoAsync(token)).AddTo(ref DisposableBag);
        Redo = new ReactiveCommand((_, token) => RedoAsync(token)).AddTo(ref DisposableBag);
        _store.LoadUndoRedo(_undoStack.Push, _redoStack.Push);
        _undoStack
            .ObserveCountChanged(true)
            .Subscribe(c => Undo.ChangeCanExecute(c != 0))
            .AddTo(ref DisposableBag);
        _redoStack
            .ObserveCountChanged(true)
            .Subscribe(c => Redo.ChangeCanExecute(c != 0))
            .AddTo(ref DisposableBag);
        owner.Subscribe<TBase, UndoEvent<TBase>>(TryAddToHistory).AddTo(ref DisposableBag);
    }

    public ReactiveCommand Undo { get; }

    public async ValueTask UndoAsync(CancellationToken cancel = default)
    {
        if (_undoStack.TryPop(out var snapshot))
        {
            var contextPath = snapshot.Path;
            var target = await _owner.NavigateByPath(contextPath);
            var handler = target.Undo.Find(snapshot.ChangeId);
            var change = handler.Create();
            _store.LoadChange(snapshot, change);
            await handler.Undo(change, cancel);
            _redoStack.Push(snapshot);
        }
    }

    public IObservableCollection<IUndoSnapshot<TId>> UndoStack => _undoStack;
    public ReactiveCommand Redo { get; }

    public async ValueTask RedoAsync(CancellationToken cancel = default)
    {
        if (_redoStack.TryPop(out var snapshot))
        {
            var contextPath = snapshot.Path;
            var target = await _owner.NavigateByPath(contextPath);
            var handler = target.Undo.Find(snapshot.ChangeId);
            var change = handler.Create();
            _store.LoadChange(snapshot, change);
            await handler.Redo(change, cancel);
            _undoStack.Push(snapshot);
        }
    }

    public IObservableCollection<IUndoSnapshot<TId>> RedoStack => _redoStack;

    private ValueTask TryAddToHistory(TBase x, UndoEvent<TBase> e)
    {
        var path = e.Sender.GetPathFrom<TBase, TId>(_owner);
        var snapshot = _store.CreateSnapshot(path, e.ChangeId);
        _store.SaveChange(snapshot, e.Change); // TODO: move serialization to separate thread
        _undoStack.Push(snapshot);
        _redoStack.Clear();
        return ValueTask.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _store.SaveUndoRedo(_undoStack, _redoStack);
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        _store.SaveUndoRedo(_undoStack, _redoStack);
        await base.DisposeAsyncCore();
    }
}
