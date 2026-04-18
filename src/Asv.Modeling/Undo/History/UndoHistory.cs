using Asv.Common;
using ObservableCollections;
using R3;

namespace Asv.Modeling;

public class UndoHistory<TBase, TId> : AsyncDisposableOnceBag, IUndoHistory<TBase>
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
{
    private readonly TBase _owner;
    private readonly IUndoHistoryStore _store;
    private readonly ObservableStack<IUndoSnapshot> _undoStack = new();
    private readonly ObservableStack<IUndoSnapshot> _redoStack = new();

    public UndoHistory(TBase owner, IUndoHistoryStore store)
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
     
        owner
            .Subscribe<TBase, UndoEvent<TBase>>(TryAddToHistory)
            .AddTo(ref DisposableBag);
    }

    public ReactiveCommand Undo { get; }

    public async ValueTask UndoAsync(CancellationToken cancel = default)
    {
        if (_undoStack.TryPop(out var snapshot))
        {
            var contextPath = snapshot.Path;
            var target = await _owner.NavigateByPath(contextPath) as ISupportUndo<TBase>;
            if (target == null)
            {
                throw new Exception($"Target {target} not support undo or not found");
            }
            var handler = target.Undo.Find(snapshot.ChangeId);
            var change = handler.Create();
            _store.LoadChange(snapshot, change);
            await handler.Undo(change, cancel);
            _redoStack.Push(snapshot);
        }
    }

    public IObservableCollection<IUndoSnapshot> UndoStack => _undoStack;
    public ReactiveCommand Redo { get; }

    public async ValueTask RedoAsync(CancellationToken cancel = default)
    {
        if (_redoStack.TryPop(out var snapshot))
        {
            var contextPath = snapshot.Path;
            var target = await _owner.NavigateByPath(contextPath) as ISupportUndo<TBase>;
            if (target == null)
            {
                throw new Exception($"Target {target} not support undo or not found");
            }
            var handler = target.Undo.Find(snapshot.ChangeId);
            var change = handler.Create();
            _store.LoadChange(snapshot, change);
            await handler.Redo(change, cancel);
            _undoStack.Push(snapshot);
        }
    }

    public IObservableCollection<IUndoSnapshot> RedoStack => _redoStack;

    private ValueTask TryAddToHistory(TBase x, UndoEvent<TBase> e)
    {
        var path = e.Sender.GetPathFrom<TBase, NavId>(_owner);
        var snapshot = _store.CreateSnapshot(new NavPath(path), e.ChangeId);
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