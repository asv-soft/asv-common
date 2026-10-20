using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using R3;
using Xunit;

namespace Asv.Common.Test;

public class TimeProviderLifetimeRegressionTest
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task LinkIndicatorDispose_StopsOwnedTimer(bool observable, bool asyncDispose)
    {
        var time = new RecordingTimeProvider();
        using var source = new Subject<int>();
        var indicator = observable
            ? new TimeBasedObservableLinkIndicator<int>(
                source,
                TimeSpan.FromSeconds(1),
                timeProvider: time
            )
            : (TimeBasedLinkIndicatorBase)
                new TimeBasedLinkIndicator(TimeSpan.FromSeconds(1), timeProvider: time);
        source.OnNext(1);
        Assert.Single(time.Timers);

        if (asyncDispose)
        {
            await indicator.DisposeAsync();
        }
        else
        {
            indicator.Dispose();
        }

        Assert.True(time.Timers[0].IsDisposed);
        source.OnNext(2);
        time.Advance(TimeSpan.FromSeconds(10));
        Assert.Throws<ObjectDisposedException>(() => indicator.State.Subscribe(_ => { }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelAfter_DisposedSourceDoesNotThrowWhenTimerExpires(bool milliseconds)
    {
        var time = new RecordingTimeProvider();
        var source = new CancellationTokenSource();
        if (milliseconds)
        {
            source.CancelAfter(1000, time);
        }
        else
        {
            source.CancelAfter(TimeSpan.FromSeconds(1), time);
        }
        source.Dispose();

        time.Advance(TimeSpan.FromSeconds(1));

        Assert.True(Assert.Single(time.Timers).IsDisposed);
    }

    [Fact]
    public void CancelAfter_LiveSourceCancelsAndStopsTimer()
    {
        var time = new RecordingTimeProvider();
        using var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromSeconds(2), time);
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.False(source.IsCancellationRequested);

        time.Advance(TimeSpan.FromSeconds(1));

        Assert.True(source.IsCancellationRequested);
        Assert.True(Assert.Single(time.Timers).IsDisposed);
    }

    [Fact]
    public void CancelAfter_TimerDoesNotRootAbandonedSource()
    {
        var time = new RecordingTimeProvider();
        var source = ScheduleAbandonedSource(time);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(source.IsAlive);
        time.Advance(TimeSpan.FromHours(1));
        Assert.True(Assert.Single(time.Timers).IsDisposed);
        GC.KeepAlive(time);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference ScheduleAbandonedSource(TimeProvider time)
    {
        var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromHours(1), time);
        return new WeakReference(source);
    }

    private sealed class RecordingTimeProvider : TimeProvider
    {
        private readonly FakeTimeProvider _inner = new();
        public List<RecordingTimer> Timers { get; } = [];
        public override long TimestampFrequency => _inner.TimestampFrequency;

        public override long GetTimestamp() => _inner.GetTimestamp();

        public override DateTimeOffset GetUtcNow() => _inner.GetUtcNow();

        public void Advance(TimeSpan value) => _inner.Advance(value);

        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period
        )
        {
            var timer = new RecordingTimer(_inner.CreateTimer(callback, state, dueTime, period));
            Timers.Add(timer);
            return timer;
        }
    }

    private sealed class RecordingTimer(ITimer inner) : ITimer
    {
        public bool IsDisposed { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period) => inner.Change(dueTime, period);

        public void Dispose()
        {
            IsDisposed = true;
            inner.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            IsDisposed = true;
            await inner.DisposeAsync();
        }
    }
}
