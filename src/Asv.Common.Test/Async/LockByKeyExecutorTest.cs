using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Asv.Common.Test.Async;

public class LockByKeyExecutorTest
{
    private static readonly TimeSpan FailureTimeout = TimeSpan.FromSeconds(5);
    private readonly ITestOutputHelper _testOutputHelper;

    public LockByKeyExecutorTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Check_For_ArgumentNullException_When_Constructor_Parameter_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            var locker = new LockByKeyExecutor<string>(null!);
        });
    }

    [Fact]
    public void Check_For_ArgumentNullException_When_Execute_Method_Parameter_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            var locker = new LockByKeyExecutor<string>();
            locker.Execute(null!, () => { });
        });

        Assert.Throws<ArgumentNullException>(() =>
        {
            var locker = new LockByKeyExecutor<string>();
            locker.Execute("test", null!);
        });

        Assert.Throws<ArgumentNullException>(() =>
        {
            var locker = new LockByKeyExecutor<string>();
            locker.Execute<string>("test", null!);
        });

        Assert.Throws<ArgumentNullException>(() =>
        {
            var locker = new LockByKeyExecutor<string>();
            locker.Execute<string>(null!, () => null!);
        });
    }

    [Fact]
    public async Task Check_For_Same_Key_Execution()
    {
        var locker = new LockByKeyExecutor<string>();
        var cancel = TestContext.Current.CancellationToken;
        var firstEntered = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var secondStarted = new TaskCompletionSource<Thread>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        using var releaseFirst = new ManualResetEventSlim(false);
        using var watchdog = new CancellationTokenSource(FailureTimeout, TimeProvider.System);
        using var waitCancel = CancellationTokenSource.CreateLinkedTokenSource(
            cancel,
            watchdog.Token
        );
        var executionCount = 0;
        var firstExited = 0;
        var secondEntered = 0;

        var first = StartWorker(() =>
            locker.Execute(
                "one",
                () =>
                {
                    Interlocked.Increment(ref executionCount);
                    firstEntered.TrySetResult();
                    Assert.True(releaseFirst.Wait(FailureTimeout, cancel));
                    Volatile.Write(ref firstExited, 1);
                }
            )
        );
        Task? second = null;
        try
        {
            await firstEntered.Task.WaitAsync(waitCancel.Token);
            second = StartWorker(() =>
            {
                secondStarted.TrySetResult(Thread.CurrentThread);
                locker.Execute(
                    "one",
                    () =>
                    {
                        Volatile.Write(ref secondEntered, 1);
                        Assert.Equal(1, Volatile.Read(ref firstExited));
                        Interlocked.Increment(ref executionCount);
                    }
                );
            });
            var secondThread = await secondStarted.Task.WaitAsync(waitCancel.Token);

            // Wait for actual lock contention, not for a guessed scheduling delay.
            while (
                (secondThread.ThreadState & ThreadState.WaitSleepJoin) == 0 && !second.IsCompleted
            )
            {
                waitCancel.Token.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            Assert.Equal(0, Volatile.Read(ref secondEntered));
            Assert.False(second.IsCompleted);
        }
        finally
        {
            releaseFirst.Set();
            await (second is null ? first : Task.WhenAll(first, second)).WaitAsync(FailureTimeout);
        }

        Assert.Equal(2, executionCount);
    }

    [Fact]
    public async Task Check_For_Different_Key_Execution()
    {
        var locker = new LockByKeyExecutor<string>();
        var cancel = TestContext.Current.CancellationToken;
        using var bothEntered = new CountdownEvent(2);
        using var release = new ManualResetEventSlim(false);

        void Execute(string key)
        {
            locker.Execute(
                key,
                () =>
                {
                    bothEntered.Signal();
                    Assert.True(release.Wait(FailureTimeout, cancel));
                }
            );
        }

        var first = StartWorker(() => Execute("one"));
        var second = StartWorker(() => Execute("two"));
        try
        {
            // Both callbacks must enter before either is allowed to finish.
            Assert.True(bothEntered.Wait(FailureTimeout, cancel));
        }
        finally
        {
            release.Set();
            await Task.WhenAll(first, second).WaitAsync(FailureTimeout);
        }
    }

    private static Task StartWorker(Action action) =>
        Task.Factory.StartNew(
            action,
            TestContext.Current.CancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );
}
