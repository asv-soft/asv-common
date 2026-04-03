using R3;

namespace Asv.Common;

public static class UndoControllerMixin
{
    public static IDisposable Register<T>(
        this IUndoController controller,
        string name,
        ReactiveProperty<T> prop
    )
    {
        return controller.Register(new ReactivePropertyChangeHandler<T>(name, prop));
    }
}
