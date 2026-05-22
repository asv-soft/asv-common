namespace Asv.Modeling;

/// <summary>
/// Provides convenience registration overloads for <see cref="IUndoController"/>.
/// </summary>
public static class UndoControllerMixin
{
    extension(IUndoController controller)
    {
        /// <summary>
        /// Registers asynchronous undo and redo callbacks using a parameterless change constructor.
        /// </summary>
        /// <typeparam name="TChange">The concrete undo change type.</typeparam>
        /// <param name="changeId">Unique identifier of the change registration within the controller.</param>
        /// <param name="undo">Callback that reverts a change.</param>
        /// <param name="redo">Callback that reapplies a change.</param>
        /// <returns>A sink used by the registered member to publish changes.</returns>
        public IUndoChangeSink<TChange> Register<TChange>(
            string changeId,
            AsyncUndoCallback<TChange> undo,
            AsyncUndoCallback<TChange> redo
        )
            where TChange : IUndoChange, new()
        {
            return controller.Register(changeId, undo, redo, static () => new TChange());
        }

        /// <summary>
        /// Registers synchronous undo and redo callbacks with an explicit change factory.
        /// </summary>
        /// <typeparam name="TChange">The concrete undo change type.</typeparam>
        /// <param name="changeId">Unique identifier of the change registration within the controller.</param>
        /// <param name="undo">Callback that reverts a change.</param>
        /// <param name="redo">Callback that reapplies a change.</param>
        /// <param name="factory">Factory used to create empty changes for history deserialization.</param>
        /// <returns>A sink used by the registered member to publish changes.</returns>
        public IUndoChangeSink<TChange> Register<TChange>(
            string changeId,
            UndoCallback<TChange> undo,
            UndoCallback<TChange> redo,
            Func<TChange> factory
        )
            where TChange : IUndoChange
        {
            return controller.Register(
                changeId,
                (change, _) =>
                {
                    undo(change);
                    return ValueTask.CompletedTask;
                },
                (change, _) =>
                {
                    redo(change);
                    return ValueTask.CompletedTask;
                },
                factory
            );
        }

        /// <summary>
        /// Registers synchronous undo and redo callbacks using a parameterless change constructor.
        /// </summary>
        /// <typeparam name="TChange">The concrete undo change type.</typeparam>
        /// <param name="changeId">Unique identifier of the change registration within the controller.</param>
        /// <param name="undo">Callback that reverts a change.</param>
        /// <param name="redo">Callback that reapplies a change.</param>
        /// <returns>A sink used by the registered member to publish changes.</returns>
        public IUndoChangeSink<TChange> Register<TChange>(
            string changeId,
            UndoCallback<TChange> undo,
            UndoCallback<TChange> redo
        )
            where TChange : IUndoChange, new()
        {
            return controller.Register(changeId, undo, redo, static () => new TChange());
        }
    }
}
