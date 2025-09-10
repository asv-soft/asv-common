using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Asv.Common
{
    public abstract class DisposableOnce : IDisposable
    {
        private int _isDisposed;

        #region Disposing

        protected bool IsDisposed => Volatile.Read(ref _isDisposed) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _isDisposed) == 0)
            {
                return;
            }

            throw new ObjectDisposedException(this?.GetType().FullName);
        }

        public void Dispose()
        {
            // Make sure we're the first call to Dispose
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
            {
                return;
            }
            /* We didn't use the following pattern:
            protected virtual void Dispose(bool disposing)
            {
                if(disposing)
                {
                    // dispose managed resources
                }
                // dispose unmanaged resources
            }*/

            // in real-world scenarios, we almost never encounter unmanaged resources, and in this case, half of the pattern is effectively redundant.
            InternalDisposeOnce();
            GC.SuppressFinalize(this);
        }

        protected abstract void InternalDisposeOnce();

        #endregion
    }
}
