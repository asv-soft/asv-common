using System;
using System.Diagnostics;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Asv.Common.Test.Async;

public class LockByKeyExecutorTest
{
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
            var locker = new LockByKeyExecutor<string>(null);
        });
    }

    [Fact]
    public void Check_For_ArgumentNullException_When_Execute_Method_Parameter_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            var locker = new LockByKeyExecutor<string>();
            locker.Execute(null, () => { });
        });

        Assert.Throws<ArgumentNullException>(() =>
        {
            var locker = new LockByKeyExecutor<string>();
            locker.Execute("test", null);
        });

        Assert.Throws<ArgumentNullException>(() =>
        {
            var locker = new LockByKeyExecutor<string>();
            locker.Execute<string>("test", null);
        });

        Assert.Throws<ArgumentNullException>(() =>
        {
            var locker = new LockByKeyExecutor<string>();
            locker.Execute<string>(null, () => null);
        });
    }

    [Fact]
    public void Check_For_Same_Key_Execution()
    {
        var locker = new LockByKeyExecutor<string>();
        var threads = new Thread[2];
        var firstStopWatch = new Stopwatch();
        var secondStopWatch = new Stopwatch();

        threads[0] = new Thread(() =>
        {
            locker.Execute("one", () => Thread.Sleep(100));
            firstStopWatch.Stop();
        });
        threads[1] = new Thread(() =>
        {
            locker.Execute("one", () => Thread.Sleep(100));
            secondStopWatch.Stop();
        });

        firstStopWatch.Start();
        secondStopWatch.Start();
        threads[0].Start();
        threads[1].Start();

        foreach (var thread in threads)
        {
            thread.Join();
        }

        Assert.True(
            secondStopWatch.ElapsedMilliseconds - firstStopWatch.ElapsedMilliseconds >= 100
        );
    }

    [Fact]
    public void Check_For_Different_Key_Execution()
    {
        var locker = new LockByKeyExecutor<string>();
        var threads = new Thread[2];
        var firstStopWatch = new Stopwatch();
        var secondStopWatch = new Stopwatch();

        threads[0] = new Thread(() =>
        {
            locker.Execute("one", () => Thread.Sleep(1000));
            firstStopWatch.Stop();
        });
        threads[1] = new Thread(() =>
        {
            locker.Execute("two", () => Thread.Sleep(1000));
            secondStopWatch.Stop();
        });

        firstStopWatch.Start();
        secondStopWatch.Start();
        threads[0].Start();
        threads[1].Start();

        foreach (var thread in threads)
        {
            thread.Join();
        }

        Assert.True(
            secondStopWatch.ElapsedMilliseconds - firstStopWatch.ElapsedMilliseconds < 1000
        );
    }
}
