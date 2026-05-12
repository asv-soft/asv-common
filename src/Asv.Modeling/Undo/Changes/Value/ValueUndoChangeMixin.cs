namespace Asv.Modeling;

using R3;

/// <summary>
/// Provides convenience methods for registering and publishing value undo changes.
/// </summary>
public static class ValueUndoChangeMixin
{
    extension(IUndoController controller)
    {
        /// <summary>
        /// Registers a reactive property value change handler and publishes property changes automatically.
        /// </summary>
        /// <typeparam name="T">The type of property value affected by the change.</typeparam>
        /// <param name="changeId">Unique identifier of the change registration within the controller.</param>
        /// <param name="prop">The reactive property to track.</param>
        /// <returns>A disposable subscription that unregisters the handler and property listener.</returns>
        public IDisposable Create<T>(string changeId, ReactiveProperty<T> prop)
        {
            var publisher = controller.CreateValueChange<T>(
                changeId,
                value => prop.Value = value,
                value => prop.Value = value
            );
            var subscription = prop.Pairwise()
                .Subscribe(change => publisher.Publish(change.Previous, change.Current));
            return Disposable.Combine(publisher, subscription);
        }

        /// <summary>
        /// Registers a value change handler that maps undo and redo to asynchronous value callbacks.
        /// </summary>
        /// <typeparam name="T">The type of value affected by the change.</typeparam>
        /// <param name="changeId">Unique identifier of the change registration within the controller.</param>
        /// <param name="undo">Callback that receives the old value when the change is undone.</param>
        /// <param name="redo">Callback that receives the new value when the change is redone.</param>
        /// <returns>A sink used to publish value changes for this registration.</returns>
        public IUndoChangeSink<ValueUndoChange<T>> CreateValueChange<T>(
            string changeId,
            Func<T, CancellationToken, ValueTask> undo,
            Func<T, CancellationToken, ValueTask> redo
        )
        {
            return controller.Create(
                changeId,
                (change, cancel) => undo(change.OldValue, cancel),
                (change, cancel) => redo(change.NewValue, cancel),
                static () => new ValueUndoChange<T>()
            );
        }

        /// <summary>
        /// Registers a value change handler that maps undo and redo to synchronous value callbacks.
        /// </summary>
        /// <typeparam name="T">The type of value affected by the change.</typeparam>
        /// <param name="changeId">Unique identifier of the change registration within the controller.</param>
        /// <param name="undo">Callback that receives the old value when the change is undone.</param>
        /// <param name="redo">Callback that receives the new value when the change is redone.</param>
        /// <returns>A sink used to publish value changes for this registration.</returns>
        public IUndoChangeSink<ValueUndoChange<T>> CreateValueChange<T>(
            string changeId,
            Action<T> undo,
            Action<T> redo
        )
        {
            return controller.Create(
                changeId,
                (change, _) =>
                {
                    undo(change.OldValue);
                    return ValueTask.CompletedTask;
                },
                (change, cancel) =>
                {
                    redo(change.NewValue);
                    return ValueTask.CompletedTask;
                },
                static () => new ValueUndoChange<T>()
            );
        }
    }

    extension<T>(IUndoChangeSink<ValueUndoChange<T>> sink)
    {
        /// <summary>
        /// Publishes an update change with the specified old and new values.
        /// </summary>
        /// <param name="oldValue">The value before the change.</param>
        /// <param name="newValue">The value after the change.</param>
        public void Publish(T oldValue, T newValue)
        {
            sink.Publish(ChangeOperation.Update, oldValue, newValue);
        }

        /// <summary>
        /// Publishes a change with the specified operation, old value, and new value.
        /// </summary>
        /// <param name="operation">The logical operation represented by the change.</param>
        /// <param name="oldValue">The value before the change.</param>
        /// <param name="newValue">The value after the change.</param>
        public void Publish(ChangeOperation operation, T oldValue, T newValue)
        {
            ArgumentNullException.ThrowIfNull(sink);
            sink.Publish(
                new ValueUndoChange<T>
                {
                    Operation = operation,
                    OldValue = oldValue,
                    NewValue = newValue,
                }
            );
        }

        /// <summary>
        /// Publishes an update change from an old/new value tuple.
        /// </summary>
        /// <param name="change">The old and new values.</param>
        public void Publish((T OldValue, T NewValue) change)
        {
            sink.Publish(change.OldValue, change.NewValue);
        }

        /// <summary>
        /// Publishes a change from an operation and old/new value tuple.
        /// </summary>
        /// <param name="operation">The logical operation represented by the change.</param>
        /// <param name="change">The old and new values.</param>
        public void Publish(ChangeOperation operation, (T OldValue, T NewValue) change)
        {
            sink.Publish(operation, change.OldValue, change.NewValue);
        }
    }
}
