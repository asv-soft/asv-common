using System;
using System.Threading;

namespace Asv.Common
{
    public abstract class DisposableOnce : IDisposable
    {
        private const int Disposed = 1;
        private const int NotDisposed = 0;
        private int _disposeFlag;

        #region Disposing

        protected bool IsDisposed => Thread.VolatileRead(ref _disposeFlag) > 0;

        protected void ThrowIfDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().Name);
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposeFlag, Disposed, NotDisposed) != NotDisposed) return;
            InternalDisposeOnce();
            GC.SuppressFinalize(this);
        }

        protected abstract void InternalDisposeOnce();

        #endregion
    }
}
