using System;
using System.Threading.Tasks;
using R3;

namespace Asv.Common;

public class ProxyLinkIndicator : AsyncDisposableOnce, ILinkIndicator
{
    private readonly ReactiveProperty<LinkState> _state;
    private readonly Subject<Unit> _onFound;
    private readonly Subject<Unit> _onLost;
    private IDisposable? _sub1;

    public ProxyLinkIndicator(LinkState initialState)
    {
        _state = new ReactiveProperty<LinkState>(initialState);
        _onFound = new Subject<Unit>();
        _onLost = new Subject<Unit>();
    }

    public ReadOnlyReactiveProperty<LinkState> State => _state;

    public void UpdateSource(ILinkIndicator origin)
    {
        _sub1?.Dispose();
        _sub1 = origin.State.Subscribe(x =>
        {
            if (_state.IsDisposed == false)
            {
                _state.Value = x;
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _state.Dispose();
            _onFound.Dispose();
            _onLost.Dispose();
            _sub1?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await CastAndDispose(_state);
        await CastAndDispose(_onFound);
        await CastAndDispose(_onLost);
        if (_sub1 != null)
        {
            await CastAndDispose(_sub1);
        }

        await base.DisposeAsyncCore();

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync();
            }
            else
            {
                resource.Dispose();
            }
        }
    }
}
