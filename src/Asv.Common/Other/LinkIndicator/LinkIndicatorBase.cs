using System;
using System.Threading.Tasks;
using R3;

namespace Asv.Common;

public class LinkIndicatorBase(int downgradeErrors = 3) : AsyncDisposableOnce, ILinkIndicator
{
    private int _connErrors;
    private readonly object _sync = new();
    private readonly ReactiveProperty<LinkState> _state = new(LinkState.Disconnected);


    protected virtual void InternalUpgrade()
    {
        if (IsDisposed) return;
        lock (_sync)
        {
            _connErrors = 0;
            _state.Value = LinkState.Connected;
        }
    }

    protected void InternalDowngrade()
    {
        if (IsDisposed) return;
        lock (_sync)
        {
            _connErrors++;
            if (_connErrors >= 1 && _connErrors <= downgradeErrors) _state.Value = LinkState.Downgrade;
            if (_connErrors >= downgradeErrors) _state.Value = LinkState.Disconnected;
        }
    }
        
    public void ForceDisconnected()
    {
        if (IsDisposed) return;
        _state.Value = LinkState.Disconnected;
    }

    public ReadOnlyReactiveProperty<LinkState> State => _state;

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _state.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_state is IAsyncDisposable stateAsyncDisposable)
            await stateAsyncDisposable.DisposeAsync();
        else
            _state.Dispose();

        await base.DisposeAsyncCore();
    }

    #endregion
}