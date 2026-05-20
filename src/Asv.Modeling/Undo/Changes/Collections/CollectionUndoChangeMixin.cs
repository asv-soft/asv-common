namespace Asv.Modeling;

using ObservableCollections;
using R3;

/// <summary>
/// Provides convenience methods for registering and publishing collection undo changes.
/// </summary>
public static class CollectionUndoChangeMixin
{
    extension(IUndoController controller)
    {
        /// <summary>
        /// Registers an observable list change handler and publishes collection changes automatically.
        /// </summary>
        /// <typeparam name="T">The type of items stored in the collection.</typeparam>
        /// <param name="changeId">Unique identifier of the change registration within the controller.</param>
        /// <param name="list">The observable list to track.</param>
        /// <returns>A disposable subscription that unregisters the handler and collection listener.</returns>
        public IDisposable Create<T>(string changeId, ObservableList<T> list)
        {
            var publisher = controller.Register(
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
                publisher.Publish(args);
            }

            list.CollectionChanged += OnCollectionChanged;
            var subscription = Disposable.Create(() =>
                list.CollectionChanged -= OnCollectionChanged
            );
            return Disposable.Combine(publisher, subscription);
        }
    }

    extension<T>(IUndoChangeSink<CollectionUndoChange<T>> sink)
    {
        /// <summary>
        /// Publishes a collection undo change from observable collection event arguments.
        /// </summary>
        /// <param name="args">The observable collection change event arguments.</param>
        public void Publish(in NotifyCollectionChangedEventArgs<T> args)
        {
            ArgumentNullException.ThrowIfNull(sink);
            sink.Publish(CollectionUndoChange<T>.From(args));
        }
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

    private static void InsertRange<T>(ObservableList<T> list, int index, IReadOnlyList<T> items)
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

    private static void ReplaceRange<T>(ObservableList<T> list, int index, IReadOnlyList<T> items)
    {
        for (var i = 0; i < items.Count; i++)
        {
            list[index + i] = items[i];
        }
    }
}
