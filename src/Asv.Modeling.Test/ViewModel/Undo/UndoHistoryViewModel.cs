using Asv.Common;
using R3;

namespace Asv.Modeling;

public abstract class UndoHistoryViewModel : ViewModelBase, IHasUndoHistory<IViewModel>
{
    protected UndoHistoryViewModel(string typeId, string storageDirectory, NavArgs args = default)
        : base(typeId, args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageDirectory);
        UndoHistory = new UndoHistory<IViewModel>(
            this,
            new JsonUndoHistoryStore(storageDirectory)
        ).AddTo(ref DisposableBag);
    }

    public IUndoHistory<IViewModel> UndoHistory { get; }
}
