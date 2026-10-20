using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Asv.Common.Test;

public class AsyncDisposableOnceBagWithCancelTest
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Dispose_ReleasesBagOnce(bool asyncDispose, bool createCancel)
    {
        await using var subject = new TestLifetime();
        var events = new List<string>();
        var token = createCancel ? subject.Token : CancellationToken.None;
        using var registration = token.Register(() => events.Add("cancel"));
        subject.Add(new TrackedDisposable(() => events.Add("first")));
        subject.Add(new TrackedDisposable(() => events.Add("second")));

        await DisposeAsync(subject, asyncDispose);
        subject.Dispose();
        await subject.DisposeAsync();

        Assert.True(subject.IsDisposed);
        Assert.Equal(
            createCancel ? ["cancel", "first", "second"] : new[] { "first", "second" },
            events
        );
        if (createCancel)
        {
            Assert.True(token.IsCancellationRequested);
            Assert.Throws<ObjectDisposedException>(() => token.WaitHandle);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dispose_CancellationCallbackThrows_StillReleasesResources(bool asyncDispose)
    {
        await using var subject = new TestLifetime();
        var token = subject.Token;
        var failure = new InvalidOperationException("Cancellation callback failed.");
        var callbackCount = 0;
        var disposeCount = 0;
        using var successfulRegistration = token.Register(() => callbackCount++);
        using var throwingRegistration = token.Register(() => throw failure);
        subject.Add(new TrackedDisposable(() => disposeCount++));

        var exception = await Assert.ThrowsAsync<AggregateException>(() =>
            DisposeAsync(subject, asyncDispose)
        );
        subject.Dispose();
        await subject.DisposeAsync();

        Assert.Same(failure, Assert.Single(exception.InnerExceptions));
        Assert.Equal(1, callbackCount);
        Assert.Equal(1, disposeCount);
        Assert.True(subject.IsDisposed);
        Assert.True(token.IsCancellationRequested);
        Assert.Throws<ObjectDisposedException>(() => token.WaitHandle);
    }

    private static async Task DisposeAsync(TestLifetime subject, bool asyncDispose)
    {
        if (asyncDispose)
        {
            await subject.DisposeAsync();
        }
        else
        {
            subject.Dispose();
        }
    }

    private sealed class TestLifetime : AsyncDisposableOnceBagWithCancel
    {
        public CancellationToken Token => DisposeCancel;

        public void Add(IDisposable disposable) => DisposableBag.Add(disposable);
    }

    private sealed class TrackedDisposable(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
