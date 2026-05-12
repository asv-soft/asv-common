namespace Asv.Modeling;

public static class UndoControllerMixin
{
    extension(IUndoController controller)
    {
        public IUndoChangeSink<TChange> Create<TChange>(
            string changeId,
            AsyncUndoCallback<TChange> undo,
            AsyncUndoCallback<TChange> redo
        )
            where TChange : IUndoChange, new()
        {
            return controller.Create(changeId, undo, redo, static () => new TChange());
        }

        public IUndoChangeSink<TChange> Create<TChange>(
            string changeId,
            UndoCallback<TChange> undo,
            UndoCallback<TChange> redo,
            Func<TChange> factory
        )
            where TChange : IUndoChange
        {
            return controller.Create(
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

        public IUndoChangeSink<TChange> Create<TChange>(
            string changeId,
            UndoCallback<TChange> undo,
            UndoCallback<TChange> redo
        )
            where TChange : IUndoChange, new()
        {
            return controller.Create(changeId, undo, redo, static () => new TChange());
        }
    }
}
