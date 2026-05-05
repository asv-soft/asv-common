using R3;

namespace Asv.Modeling;

public static class UndoControllerMixin
{
    extension(IUndoController controller)
    {
        public void DisableChangePublication()
        {
            controller.SuppressChanges = true;
        }

        public void EnablePublication()
        {
            controller.SuppressChanges = false;
        }

        public IDisposable BeginChangePublication()
        {
            controller.SuppressChanges = true;
            return Disposable.Create(controller, x => x.EnablePublication());
        }

        public PropertyUndoHandler<T> CreateAndRegister<T>(string changeId, ReactiveProperty<T> prop)
        {
            var handler = new PropertyUndoHandler<T>(changeId, prop);
            controller.Register(handler);
            return handler;
        }
        
        public ManualUndoHandler<TChange> CreateAndRegister<TChange>(
            string changeId,
            ManualUndoHandler<TChange>.Delegate undo,
            ManualUndoHandler<TChange>.Delegate redo)
            where TChange : IChange, new()
        {
            var handler = new ManualUndoHandler<TChange>(changeId, undo, redo);
            controller.Register(handler);
            return handler;
        }
    }
}
