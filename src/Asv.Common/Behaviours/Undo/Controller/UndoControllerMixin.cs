using System;
using R3;

namespace Asv.Common;

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
    }
}
