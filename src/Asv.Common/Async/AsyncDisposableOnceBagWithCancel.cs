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
        try
        {
            if (disposing)
            {
                try
                {
                    _cancel?.Cancel(false);
                }
                finally
                {
                    _cancel?.Dispose();
                }
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        try
        {
            try
            {
                if (_cancel != null)
                {
                    await _cancel.CancelAsync();
                }
            }
            finally
            {
                _cancel?.Dispose();
            }
        }
        finally
        {
            await base.DisposeAsyncCore();
        }
    }
}
