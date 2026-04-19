using Asv.Common;

namespace Asv.Modeling;

public abstract class UndoableViewModel : ViewModelBase, ISupportUndo<IViewModel>
{
    protected UndoableViewModel(string typeId, NavArgs args = default) 
        : base(typeId, args)
    {
        Undo = new UndoController<IViewModel>(this).DisposeItWith(Disposable);
    }

    public IUndoController Undo { get; }
}
