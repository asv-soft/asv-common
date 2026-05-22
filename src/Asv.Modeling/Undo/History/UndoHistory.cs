using Asv.Common;
using ObservableCollections;
using R3;

namespace Asv.Modeling;

/// <summary>
/// Default implementation of <see cref="IUndoHistory{TBase}"/>.
/// </summary>
/// <typeparam name="TBase">The routed and navigable tree node type.</typeparam>
public class UndoHistory<TBase> : AsyncDisposableOnceBag, IUndoHistory<TBase>
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
{
    private readonly TBase _owner;
    private readonly IUndoHistoryStore _store;
    private readonly ObservableStack<IUndoSnapshot> _undoStack = new();
    private readonly ObservableStack<IUndoSnapshot> _redoStack = new();
    private readonly Lock _saveSync = new();
    private Task _saveTail = Task.CompletedTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="UndoHistory{TBase}"/> class.
    /// </summary>
    /// <param name="owner">The root node used to route undo events and resolve change targets.</param>
    /// <param name="store">The store used to persist undo history.</param>
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

        owner.Events.Catch<UndoEvent<TBase>>(TryAddToHistory).AddTo(ref DisposableBag);
    }

    /// <inheritdoc />
    public ReactiveCommand Undo { get; }

    /// <inheritdoc />
    public async ValueTask UndoAsync(CancellationToken cancel = default)
    {
        await FlushPendingSaves(cancel);
        if (_undoStack.TryPop(out var snapshot))
        {
            try
            {
                var contextPath = snapshot.Path;
                var target = await _owner.NavigateByPath(contextPath) as ISupportUndo<TBase>;
                if (target == null)
                {
                    throw new Exception(
                        $"Target '{contextPath}' does not support undo or was not found"
                    );
                }
                var handler = target.Undo[snapshot.ChangeId];
                var change = handler.Create();
                _store.LoadChange(snapshot, change);
                await handler.Undo(change, cancel);
                _redoStack.Push(snapshot);
            }
            catch
            {
                _undoStack.Push(snapshot);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public IObservableCollection<IUndoSnapshot> UndoStack => _undoStack;

    /// <inheritdoc />
    public ReactiveCommand Redo { get; }

    /// <inheritdoc />
    public async ValueTask RedoAsync(CancellationToken cancel = default)
    {
        await FlushPendingSaves(cancel);
        if (_redoStack.TryPop(out var snapshot))
        {
            try
            {
                var contextPath = snapshot.Path;
                var target = await _owner.NavigateByPath(contextPath) as ISupportUndo<TBase>;
                if (target == null)
                {
                    throw new Exception(
                        $"Target '{contextPath}' does not support redo or was not found"
                    );
                }
                var handler = target.Undo[snapshot.ChangeId];
                var change = handler.Create();
                _store.LoadChange(snapshot, change);
                await handler.Redo(change, cancel);
                _undoStack.Push(snapshot);
            }
            catch
            {
                _redoStack.Push(snapshot);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public IObservableCollection<IUndoSnapshot> RedoStack => _redoStack;

    private ValueTask TryAddToHistory(TBase x, UndoEvent<TBase> e, CancellationToken cancel)
    {
        var path = e.Sender.GetPathFrom<TBase, NavId>(_owner);
        var snapshot = _store.CreateSnapshot(new NavPath(path), e.ChangeId);
        _undoStack.Push(snapshot);
        _redoStack.Clear();
        QueueSaveChange(snapshot, e.UndoChange);
        return ValueTask.CompletedTask;
    }

    private void QueueSaveChange(IUndoSnapshot snapshot, IUndoChange undoChange)
    {
        lock (_saveSync)
        {
            _saveTail = _saveTail
                .ContinueWith(
                    async previous =>
                    {
                        await previous.ConfigureAwait(false);
                        _store.SaveChange(snapshot, undoChange);
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.Default
                )
                .Unwrap();
        }
    }

    private Task GetSaveTail()
    {
        lock (_saveSync)
        {
            return _saveTail;
        }
    }

    private async ValueTask FlushPendingSaves(CancellationToken cancel = default)
    {
        await GetSaveTail().WaitAsync(cancel).ConfigureAwait(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FlushPendingSaves().AsTask().GetAwaiter().GetResult();
            _store.SaveUndoRedo(_undoStack.Reverse(), _redoStack.Reverse());
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await FlushPendingSaves();
        _store.SaveUndoRedo(_undoStack.Reverse(), _redoStack.Reverse());
        await base.DisposeAsyncCore();
    }
}
