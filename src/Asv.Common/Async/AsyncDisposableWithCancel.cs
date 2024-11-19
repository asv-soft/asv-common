using System.Threading;
using System.Threading.Tasks;

namespace Asv.Common;

public abstract class AsyncDisposableWithCancel: AsyncDisposableOnce
{
    private CancellationTokenSource? _cancel;
    private readonly object _sync2 = new();

    protected CancellationToken DisposeCancel
    {
        get
        {
            if (_cancel != null)
            {
                return IsDisposed ? CancellationToken.None : _cancel.Token;
            }

            lock (_sync2)
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
            _cancel?.Cancel(false);
            _cancel?.Dispose();
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_cancel != null)
        {
            await _cancel.CancelAsync();
            _cancel.Dispose();
        }
    }
}