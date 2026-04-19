using Asv.Common;
using ObservableCollections;
using R3;

namespace Asv.Modeling;

public class NavigationController<TBase> : AsyncDisposableOnce, INavigationController<TBase>
    where TBase : ISupportNavigation<TBase>, ISupportRoutedEvents<TBase>
{
    private readonly TBase _owner;
    private readonly INavigationStore _store;
    private readonly ReactiveProperty<TBase?> _selectedControl;
    private readonly ReactiveProperty<NavPath> _selectedPath;
    private readonly ObservableStack<NavPath> _backwardStack = new();
    private readonly ObservableStack<NavPath> _forwardStack = new();
    private readonly IDisposable _disposeIt;

    public NavigationController(TBase owner, INavigationStore store)
    {
        _owner = owner;
        _store = store;
        var dispose = Disposable.CreateBuilder();
        _selectedControl = new ReactiveProperty<TBase?>().AddTo(ref dispose);
        _selectedPath = new ReactiveProperty<NavPath>().AddTo(ref dispose);
        
        Backward = new ReactiveCommand((_, _) => BackwardAsync()).AddTo(ref dispose);
        Forward = new ReactiveCommand((_, _) => ForwardAsync()).AddTo(ref dispose);
        _backwardStack
            .ObserveCountChanged(true)
            .Subscribe(c => Backward.ChangeCanExecute(c != 0))
            .AddTo(ref dispose);
        _forwardStack
            .ObserveCountChanged(true)
            .Subscribe(c => Forward.ChangeCanExecute(c != 0))
            .AddTo(ref dispose);

        ForceSelect(_owner);
        
        store.Load(_forwardStack.Push, _backwardStack.Push);
        
        _owner.Events.Catch<NavigateEvent<TBase>>(OnNavigateEvent).AddTo(ref dispose);
        
        _disposeIt = dispose.Build();
    }

    private async ValueTask OnNavigateEvent(TBase owner, NavigateEvent<TBase> e, CancellationToken cancel)
    {
        var previousPath = _selectedPath.Value;
        if (previousPath == e.Path)
        {
            return;
        }

        await GoTo(e.Path);
        if (!previousPath.IsEmpty)
        {
            _backwardStack.Push(previousPath);
        }

        _forwardStack.Clear();
    }

    public IObservableCollection<NavPath> BackwardStack => _backwardStack;
    public async ValueTask BackwardAsync()
    {
        if (_backwardStack.TryPop(out var path) == false)
        {
            return;
        }

        var previousPath = _selectedPath.Value;
        try
        {
            await GoTo(path);
            if (!previousPath.IsEmpty)
            {
                _forwardStack.Push(previousPath);
            }
        }
        catch
        {
            _backwardStack.Push(path);
            throw;
        }
    }

    public ReactiveCommand Backward { get; }
    public IObservableCollection<NavPath> ForwardStack => _forwardStack;
    public async ValueTask ForwardAsync()
    {
        if (_forwardStack.TryPop(out var path) == false)
        {
            return;
        }

        var previousPath = _selectedPath.Value;
        try
        {
            await GoTo(path);
            if (!previousPath.IsEmpty)
            {
                _backwardStack.Push(previousPath);
            }
        }
        catch
        {
            _forwardStack.Push(path);
            throw;
        }
    }

    public async ValueTask<TBase> GoTo(NavPath navPath)
    {
        if (navPath.Count == 0)
        {
            throw new ArgumentNullException(nameof(navPath));
        }

        if (navPath[0] != _owner.Id)
        {
            throw new ArgumentException($"{nameof(navPath)} must start with root {_owner.Id}");
        }

        var next = _owner;
        for (var i = 1; i < navPath.Count; i++)
        {
            next = await next.Navigate(navPath[i]);
        }
        ForceSelect(next);
        if (next is ISupportFocus nextWithFocus)
        {
            nextWithFocus.Focus();
        }
        return next;
    }

    public ReactiveCommand Forward { get; }
    public void ForceSelect(TBase? viewModel)
    {
        if (viewModel == null)
        {
            return;
        }
        _selectedControl.Value = viewModel;
        _selectedPath.Value = new NavPath(viewModel.GetPathFromRoot<TBase, NavId>());
    }

    public ReadOnlyReactiveProperty<TBase?> SelectedControl => _selectedControl;
    public ReadOnlyReactiveProperty<NavPath> SelectedPath => _selectedPath;
    
    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposeIt.Dispose();
            _store.Save(_forwardStack.Reverse(), _backwardStack.Reverse());
            _forwardStack.Clear();
            _backwardStack.Clear();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_disposeIt is IAsyncDisposable disposeItAsyncDisposable)
        {
            await disposeItAsyncDisposable.DisposeAsync();
        }
        else
        {
            _disposeIt.Dispose();
        }

        _store.Save(_forwardStack.Reverse(), _backwardStack.Reverse());
        _forwardStack.Clear();
        _backwardStack.Clear();
        await base.DisposeAsyncCore();
    }

    #endregion
}
