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
                    prop.Value = ((ScalarChange<T>)change).OldValue;
                    return ValueTask.CompletedTask;
                },
                (change, _) =>
                {
                    prop.Value = ((ScalarChange<T>)change).NewValue;
                    return ValueTask.CompletedTask;
                },
                static () => new ScalarChange<T>()
            );
            var subscription = prop.Pairwise()
                .Select(x => new ScalarChange<T> { OldValue = x.Previous, NewValue = x.Current })
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
                static () => new ObservableCollectionChangeEvent<T>()
            );
            var addSubscription = list.ObserveAdd()
                .Select(x => new ObservableCollectionChangeEvent<T>
                {
                    Operation = ChangeOperation.Create,
                    OldIndex = -1,
                    NewIndex = x.Index,
                    OldValue = default!,
                    NewValue = x.Value,
                })
                .Subscribe(publisher.Publish);
            var removeSubscription = list.ObserveRemove()
                .Select(x => new ObservableCollectionChangeEvent<T>
                {
                    Operation = ChangeOperation.Delete,
                    OldIndex = x.Index,
                    NewIndex = -1,
                    OldValue = x.Value,
                    NewValue = default!,
                })
                .Subscribe(publisher.Publish);
            return Disposable.Combine(publisher, addSubscription, removeSubscription);
        }

        public IUndoPublisher<TChange> Create<TChange>(
            string changeId,
            Func<TChange, CancellationToken, ValueTask> undo,
            Func<TChange, CancellationToken, ValueTask> redo
        )
            where TChange : IChange, new()
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
            ObservableCollectionChangeEvent<T> change
        )
        {
            switch (change.Operation)
            {
                case ChangeOperation.Create:
                    list.RemoveAt(change.NewIndex);
                    break;
                case ChangeOperation.Delete:
                    list.Insert(change.OldIndex, change.OldValue);
                    break;
                case ChangeOperation.Update:
                    list[change.OldIndex] = change.OldValue;
                    break;
                case ChangeOperation.Read:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(change),
                        change.Operation,
                        "Unknown collection change operation"
                    );
            }
        }

        private static void ApplyCollectionRedo<T>(
            ObservableList<T> list,
            ObservableCollectionChangeEvent<T> change
        )
        {
            switch (change.Operation)
            {
                case ChangeOperation.Create:
                    list.Insert(change.NewIndex, change.NewValue);
                    break;
                case ChangeOperation.Delete:
                    list.RemoveAt(change.OldIndex);
                    break;
                case ChangeOperation.Update:
                    list[change.NewIndex] = change.NewValue;
                    break;
                case ChangeOperation.Read:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(change),
                        change.Operation,
                        "Unknown collection change operation"
                    );
            }
        }
    }
}
