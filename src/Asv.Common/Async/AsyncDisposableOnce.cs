using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.Common;

public abstract class AsyncDisposableOnce : IDisposable, IAsyncDisposable
{
    private volatile int _isDisposed;

    #region Disposing
    public bool IsDisposed => _isDisposed != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfDisposed()
    {
        if (_isDisposed == 0) return;
        throw new ObjectDisposedException(this?.GetType().FullName); 
    }

    public void Dispose()
    {
        // Make sure we're the first call to Dispose
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
        {
            return;
        }
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        // Make sure we're the first call to Dispose
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
        {
            return;
        }
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }
        
    #endregion
}