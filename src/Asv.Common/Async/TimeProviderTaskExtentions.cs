using System;
using System.Threading;

namespace Asv.Common;

public static class TimeProviderTaskExtensions
{
    /// <summary>
    /// Cancels the source after the specified delay using the provided time provider.
    /// </summary>
    /// <remarks>
    /// Disposing the source does not notify an external provider's timer. For an owned timeout
    /// scope, construct CancellationTokenSource with the delay and provider so disposal stops
    /// its timer immediately. This compatibility helper does not retain the source.
    /// </remarks>
    public static void CancelAfter(
        this CancellationTokenSource cts,
        TimeSpan delay,
        TimeProvider timeProvider
    )
    {
        ArgumentNullException.ThrowIfNull(cts);
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (timeProvider == TimeProvider.System)
        {
            cts.CancelAfter(delay);
            return;
        }

        var token = cts.Token;
        var state = new CancellationTimer(cts);
        state.Start(timeProvider, delay);
        token.Register(static value => ((CancellationTimer)value!).Dispose(), state);
    }

    /// <summary>
    /// Cancels the source after the specified delay in milliseconds using the provided provider.
    /// </summary>
    public static void CancelAfter(
        this CancellationTokenSource cts,
        int delayMs,
        TimeProvider timeProvider
    ) => CancelAfter(cts, TimeSpan.FromMilliseconds(delayMs), timeProvider);

    private sealed class CancellationTimer(CancellationTokenSource source) : IDisposable
    {
        private readonly WeakReference<CancellationTokenSource> _source = new(source);
        private ITimer? _timer;

        public void Start(TimeProvider provider, TimeSpan delay)
        {
            _timer = provider.CreateTimer(
                static value => ((CancellationTimer)value!).Cancel(),
                this,
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan
            );
            try
            {
                _timer.Change(delay, Timeout.InfiniteTimeSpan);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void Cancel()
        {
            try
            {
                if (_source.TryGetTarget(out var source))
                {
                    source.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
                // The caller can dispose the source before an external timer fires.
            }
            finally
            {
                Dispose();
            }
        }

        public void Dispose() => Interlocked.Exchange(ref _timer, null)?.Dispose();
    }
}
