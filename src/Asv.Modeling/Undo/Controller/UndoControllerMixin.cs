using ObservableCollections;
using R3;

namespace Asv.Modeling;

public static class UndoControllerMixin
{
    extension(IUndoController controller)
    {
        public IDisposable Create<T>(string changeId, ReactiveProperty<T> prop)
        {
            var publisher = controller.Create(
                changeId,
                (change, _) =>
                {
                    prop.Value = ((UndoChange<T>)change).OldValue;
                    return ValueTask.CompletedTask;
                },
                (change, _) =>
                {
                    prop.Value = ((UndoChange<T>)change).NewValue;
                    return ValueTask.CompletedTask;
                },
                static () => new UndoChange<T>()
            );
            var subscription = prop.Pairwise()
                .Select(x => new UndoChange<T> { OldValue = x.Previous, NewValue = x.Current })
                .Subscribe(publisher.Publish);
            return Disposable.Combine(publisher, subscription);
        }

        public IDisposable Create<T>(string changeId, ObservableList<T> list)
        {
            var publisher = controller.Create(
                changeId,
                (change, _) =>
                {
                    ApplyCollectionUndo(list, change);
                    return ValueTask.CompletedTask;
                },
                (change, _) =>
                {
                    ApplyCollectionRedo(list, change);
                    return ValueTask.CompletedTask;
                },
                static () => new CollectionUndoChange<T>()
            );
            void OnCollectionChanged(in NotifyCollectionChangedEventArgs<T> args)
            {
                publisher.Publish(CollectionUndoChange<T>.From(args));
            }

            list.CollectionChanged += OnCollectionChanged;
            var subscription = Disposable.Create(() =>
                list.CollectionChanged -= OnCollectionChanged
            );
            return Disposable.Combine(publisher, subscription);
        }

        public IUndoChangeSink<TChange> Create<TChange>(
            string changeId,
            Func<TChange, CancellationToken, ValueTask> undo,
            Func<TChange, CancellationToken, ValueTask> redo
        )
            where TChange : IUndoChange, new()
        {
            return controller.Create(
                changeId,
                (change, cancel) => undo((TChange)change, cancel),
                (change, cancel) => redo((TChange)change, cancel),
                static () => new TChange()
            );
        }

        private static void ApplyCollectionUndo<T>(
            ObservableList<T> list,
            CollectionUndoChange<T> undoChange
        )
        {
            switch (undoChange.Operation)
            {
                case ChangeOperation.Create:
                    RemoveRange(list, undoChange.NewStartingIndex, undoChange.NewItems.Length);
                    break;
                case ChangeOperation.Delete:
                    InsertRange(list, undoChange.OldStartingIndex, undoChange.OldItems);
                    break;
                case ChangeOperation.Update:
                    ReplaceRange(list, undoChange.OldStartingIndex, undoChange.OldItems);
                    break;
                case ChangeOperation.Read:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(undoChange),
                        undoChange.Operation,
                        "Unknown collection change operation"
                    );
            }
        }

        private static void ApplyCollectionRedo<T>(
            ObservableList<T> list,
            CollectionUndoChange<T> undoChange
        )
        {
            switch (undoChange.Operation)
            {
                case ChangeOperation.Create:
                    InsertRange(list, undoChange.NewStartingIndex, undoChange.NewItems);
                    break;
                case ChangeOperation.Delete:
                    RemoveRange(list, undoChange.OldStartingIndex, undoChange.OldItems.Length);
                    break;
                case ChangeOperation.Update:
                    ReplaceRange(list, undoChange.NewStartingIndex, undoChange.NewItems);
                    break;
                case ChangeOperation.Read:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(undoChange),
                        undoChange.Operation,
                        "Unknown collection change operation"
                    );
            }
        }

        private static void InsertRange<T>(
            ObservableList<T> list,
            int index,
            IReadOnlyList<T> items
        )
        {
            for (var i = 0; i < items.Count; i++)
            {
                list.Insert(index + i, items[i]);
            }
        }

        private static void RemoveRange<T>(ObservableList<T> list, int index, int count)
        {
            for (var i = 0; i < count; i++)
            {
                list.RemoveAt(index);
            }
        }

        private static void ReplaceRange<T>(
            ObservableList<T> list,
            int index,
            IReadOnlyList<T> items
        )
        {
            for (var i = 0; i < items.Count; i++)
            {
                list[index + i] = items[i];
            }
        }
    }
}
