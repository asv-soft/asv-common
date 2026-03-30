using System;
using R3;

namespace Asv.Common;

public class UndoManager : AsyncDisposableOnceBag, IUndoManager
{
    private readonly IUndoContextResolver _resolver;
    private readonly ReactiveProperty<bool> _canUndo;
    private readonly ReactiveProperty<bool> _canRedo;

    public UndoManager(IUndoContextResolver resolver)
    {
        _resolver = resolver;
        _canRedo = new ReactiveProperty<bool>(false).AddTo(ref DisposableBag);
        _canUndo = new ReactiveProperty<bool>(false).AddTo(ref DisposableBag);
    }

    public ReadOnlyReactiveProperty<bool> CanUndo => _canUndo;

    public ReadOnlyReactiveProperty<bool> CanRedo => _canRedo;

    public IUndoTransaction CreateTransaction(string displayName)
    {
        return new UndoTransaction(displayName, _resolver);
    }
}
