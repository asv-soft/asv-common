using System;
using System.Threading.Tasks;
using R3;

namespace Asv.Common;

public class LinkIndicatorBase : ILinkIndicator, IDisposable, IAsyncDisposable
{
    private int _connErrors;
    private readonly object _sync = new();
    private readonly ReactiveProperty<LinkState> _state = new(LinkState.Disconnected);
    private readonly int _downgradeErrors;
    public LinkIndicatorBase(int downgradeErrors = 3)
    {
        _downgradeErrors = downgradeErrors;
        OnLost = _state.Where(x=>x == LinkState.Disconnected).Select(_=>Unit.Default);
        OnFound = _state.Where(x=>x == LinkState.Connected).Select(_=>Unit.Default);
            
    }


    protected virtual void InternalUpgrade()
    {
        lock (_sync)
        {
            _connErrors = 0;
            _state.OnNext(LinkState.Connected);
        }
    }

    protected void InternalDowngrade()
    {
        lock (_sync)
        {
            _connErrors++;
            if (_connErrors >= 1 && _connErrors <= _downgradeErrors) _state.OnNext(LinkState.Downgrade);
            if (_connErrors >= _downgradeErrors) _state.OnNext(LinkState.Disconnected);
        }
    }
        
    public void ForceDisconnected()
    {
        _state.OnNext(LinkState.Disconnected);
    }

    public ReactiveProperty<LinkState> State => _state;
    public Observable<Unit> OnFound { get; }
    public Observable<Unit> OnLost { get; }

    #region Dispose

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _state.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_state is IAsyncDisposable stateAsyncDisposable)
            await stateAsyncDisposable.DisposeAsync();
        else
            _state.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    #endregion
}