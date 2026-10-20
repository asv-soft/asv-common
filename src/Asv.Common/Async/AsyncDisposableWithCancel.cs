using System;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.Common;

public abstract class AsyncDisposableWithCancel : AsyncDisposableOnce
{
    private CancellationTokenSource? _cancel;
    private readonly Lock _sync2 = new();

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

    protected void CancelDisposeToken()
    {
        try
        {
            _cancel?.Cancel(false);
        }
        catch (ObjectDisposedException)
        {
            // Another shutdown path has already cancelled and disposed this source.
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
