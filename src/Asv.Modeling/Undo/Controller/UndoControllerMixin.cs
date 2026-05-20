namespace Asv.Modeling;

public static class UndoControllerMixin
{
    extension(IUndoController controller)
    {
        public IUndoChangeSink<TChange> Register<TChange>(
            string changeId,
            AsyncUndoCallback<TChange> undo,
            AsyncUndoCallback<TChange> redo
        )
            where TChange : IUndoChange, new()
        {
            return controller.Register(changeId, undo, redo, static () => new TChange());
        }

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
