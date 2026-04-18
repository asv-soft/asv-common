using System.Threading;
using System.Threading.Tasks;

namespace Asv.Common;

public class AsyncDisposableOnceBagWithCancel : AsyncDisposableOnceBag
{
    private CancellationTokenSource? _cancel;
    private readonly Lock _gate = new();

    protected CancellationToken DisposeCancel
    {
        get
        {
            if (_cancel != null)
            {
                return IsDisposed ? CancellationToken.None : _cancel.Token;
            }

            lock (_gate)
            {
                if (_cancel != null)
                {
                    return IsDisposed ? CancellationToken.None : _cancel.Token;
                }
                _cancel = new CancellationTokenSource();
                return _cancel.Token;
            }
        }
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_cancel != null)
            {
                if (_cancel.Token.CanBeCanceled)
                {
                    _cancel.Cancel(false);
                }
                _cancel.Dispose();
            }
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_cancel != null)
        {
            if (_cancel.Token.CanBeCanceled)
            {
                await _cancel.CancelAsync();
            }
            _cancel.Dispose();
        }
    }
}