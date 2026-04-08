using R3;

namespace Asv.Modeling;

public static class UndoControllerMixin
{
    extension(IUndoController controller)
    {
        public void DisableChangePublication()
        {
            controller.MuteChanges = true;
        }

        public void EnableChangePublication()
        {
            controller.MuteChanges = false;
        }

        public IDisposable BeginChangePublication()
        {
            controller.MuteChanges = true;
            return Disposable.Create(controller, x => x.EnableChangePublication());
        }

        public IDisposable Register<T>(string name, ReactiveProperty<T> prop)
        {
            return controller.Register(new ReactivePropertyChangeHandler<T>(name, prop));
        }
    }
}
