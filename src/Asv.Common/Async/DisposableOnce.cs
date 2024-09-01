using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.Common
{
    public abstract class DisposableOnce : IDisposable 
    {
        private bool _disposed;
        private readonly object _disposingSync = new();

        #region Disposing

        // ReSharper disable once InconsistentlySynchronizedField
        protected bool IsDisposed => _disposed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfDisposed()
        {
            lock (_disposingSync)
            {
                throw new ObjectDisposedException(this?.GetType().FullName); 
            }
        }

        public void Dispose()
        {
            if(_disposed) return;
            lock(_disposingSync)
            {
                if(_disposed) return;
                _disposed = true;
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
