using System;
using System.Threading;

namespace Asv.Common;

public static class TimeProviderTaskExtensions
{
    /// <summary>
    /// Cancel the CancellationTokenSource after a specified delay using the provided TimeProvider.
    /// </summary>
    /// <param name="cts">The CancellationTokenSource to cancel.</param>
    /// <param name="delay">The delay after which cancellation should occur.</param>
    /// <param name="timeProvider">The TimeProvider instance to use for timing.</param>
    public static void CancelAfter(this CancellationTokenSource cts, TimeSpan delay, TimeProvider timeProvider)
    {
        if (timeProvider == TimeProvider.System)
        {
            cts.CancelAfter(delay);
        }
        else
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            var timer = timeProvider.CreateTimer(s => ((CancellationTokenSource)s).Cancel(), cts, delay, Timeout.InfiniteTimeSpan);
            cts.Token.Register(t => ((ITimer)t).Dispose(), timer);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }    
    }

    /// <summary>
    /// Cancel the CancellationTokenSource after a specified delay using the provided TimeProvider.
    /// </summary>
    /// <param name="cts">The CancellationTokenSource to cancel.</param>
    /// <param name="delayMs">The delay after which cancellation should occur.</param>
    /// <param name="timeProvider">The TimeProvider instance to use for timing.</param>
    public static void CancelAfter(this CancellationTokenSource cts, int delayMs, TimeProvider timeProvider)
    {
        if (timeProvider == TimeProvider.System)
        {
            cts.CancelAfter(delayMs);
        }
        else
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            var timer = timeProvider.CreateTimer(s => ((CancellationTokenSource)s).Cancel(), cts, TimeSpan.FromMilliseconds(delayMs), Timeout.InfiniteTimeSpan);
            cts.Token.Register(t => ((ITimer)t).Dispose(), timer);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }    
    }
}