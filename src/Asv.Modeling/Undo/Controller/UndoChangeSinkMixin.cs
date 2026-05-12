namespace Asv.Modeling;

public static class UndoChangeSinkMixin
{
    /// <summary>
    /// Publishes an update change with the specified old and new values.
    /// </summary>
    /// <typeparam name="T">The value type affected by the change.</typeparam>
    /// <param name="sink">The undo change sink.</param>
    /// <param name="oldValue">The value before the change.</param>
    /// <param name="newValue">The value after the change.</param>
    public static void Publish<T>(
        this IUndoChangeSink<UndoChange<T>> sink,
        T oldValue,
        T newValue
    )
    {
        sink.Publish(ChangeOperation.Update, oldValue, newValue);
    }

    /// <summary>
    /// Publishes a change with the specified operation, old value, and new value.
    /// </summary>
    /// <typeparam name="T">The value type affected by the change.</typeparam>
    /// <param name="sink">The undo change sink.</param>
    /// <param name="operation">The logical operation represented by the change.</param>
    /// <param name="oldValue">The value before the change.</param>
    /// <param name="newValue">The value after the change.</param>
    public static void Publish<T>(
        this IUndoChangeSink<UndoChange<T>> sink,
        ChangeOperation operation,
        T oldValue,
        T newValue
    )
    {
        ArgumentNullException.ThrowIfNull(sink);
        sink.Publish(
            new UndoChange<T>
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
    /// <typeparam name="T">The value type affected by the change.</typeparam>
    /// <param name="sink">The undo change sink.</param>
    /// <param name="change">The old and new values.</param>
    public static void Publish<T>(
        this IUndoChangeSink<UndoChange<T>> sink,
        (T OldValue, T NewValue) change
    )
    {
        sink.Publish(change.OldValue, change.NewValue);
    }

    /// <summary>
    /// Publishes a change from an operation and old/new value tuple.
    /// </summary>
    /// <typeparam name="T">The value type affected by the change.</typeparam>
    /// <param name="sink">The undo change sink.</param>
    /// <param name="operation">The logical operation represented by the change.</param>
    /// <param name="change">The old and new values.</param>
    public static void Publish<T>(
        this IUndoChangeSink<UndoChange<T>> sink,
        ChangeOperation operation,
        (T OldValue, T NewValue) change
    )
    {
        sink.Publish(operation, change.OldValue, change.NewValue);
    }
}
