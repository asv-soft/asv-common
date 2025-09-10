using System;
using System.Threading;
using R3;

namespace Asv.Common
{
    public abstract class DisposableOnceWithCancel : DisposableOnce
    {
        private volatile CancellationTokenSource? _cancel;
        private volatile CompositeDisposable? _dispose;
        private readonly object _sync1 = new();
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

        protected T AddToDispose<T>(T value)
            where T : IDisposable
        {
            Disposable.Add(value);
            return value;
        }

        protected CompositeDisposable Disposable
        {
            get
            {
                if (_dispose != null)
                {
                    return _dispose;
                }

                lock (_sync1)
                {
                    if (_dispose != null)
                    {
                        return _dispose;
                    }

                    var dispose = new CompositeDisposable();
                    _dispose = dispose;
                    return dispose;
                }
            }
        }

        protected override void InternalDisposeOnce()
        {
            if (_cancel?.Token.CanBeCanceled == true)
            {
                _cancel.Cancel(false);
            }

            _cancel?.Dispose();
            _dispose?.Dispose();
        }
    }
}
