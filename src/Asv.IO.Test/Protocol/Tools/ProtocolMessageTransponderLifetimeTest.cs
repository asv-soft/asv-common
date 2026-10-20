using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using R3;
using Xunit;

namespace Asv.IO.Test;

public class ProtocolMessageTransponderLifetimeTest
{
    [Fact]
    public async Task Timer_OverlappingTicksSkipWithoutUnlockingActiveSend()
    {
        var time = new DeviceLifetimeTimeProvider();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var connection = new DeviceLifetimeConnection
        {
            SendCallback = async (_, cancel) => await release.Task.WaitAsync(cancel),
        };
        var message = new ExampleMessage1();
        await using var transponder = new ProtocolMessageTransponder<ExampleMessage1>(
            message,
            packet => packet.Value1++,
            connection,
            time,
            NullLoggerFactory.Instance
        );
        var resumed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = transponder.State.Subscribe(state =>
        {
            if (state == TransponderState.Ok && connection.SendCount > 0)
            {
                resumed.TrySetResult();
            }
        });
        transponder.Start(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        time.Advance(TimeSpan.FromSeconds(1));
        time.Advance(TimeSpan.FromSeconds(3));

        Assert.Equal(1, connection.SendCount);
        Assert.Equal(1, message.Value1);
        Assert.Equal(TransponderState.Skipped, transponder.State.CurrentValue);
        release.SetResult();
        await resumed.Task.WaitAsync(TestContext.Current.CancellationToken);
        transponder.Set(packet => packet.Value2 = 9);
        Assert.Equal(9, message.Value2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dispose_ActiveSendIsCancelledBeforeResourcesAreReleased(bool asyncDispose)
    {
        var time = new DeviceLifetimeTimeProvider();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken sendToken = default;
        using var connection = new DeviceLifetimeConnection
        {
            SendCallback = async (_, cancel) =>
            {
                sendToken = cancel;
                await release.Task.WaitAsync(cancel);
            },
        };
        var transponder = new ProtocolMessageTransponder<ExampleMessage1>(
            new ExampleMessage1(),
            _ => { },
            connection,
            time,
            NullLoggerFactory.Instance
        );
        var completed = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        using var subscription = transponder.State.Subscribe(
            _ => { },
            _ => { },
            _ => completed.TrySetResult()
        );
        transponder.Start(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(1, connection.SendCount);

        if (asyncDispose)
        {
            await transponder.DisposeAsync();
        }
        else
        {
            transponder.Dispose();
        }
        await completed.Task.WaitAsync(TestContext.Current.CancellationToken);

        Assert.True(sendToken.IsCancellationRequested);
        Assert.False(transponder.IsStarted);
        Assert.Equal(0, time.ActiveTimers);
        time.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(1, connection.SendCount);
    }

    [Fact]
    public async Task Dispose_FromSendCallbackDoesNotDeadlock()
    {
        var time = new DeviceLifetimeTimeProvider();
        using var connection = new DeviceLifetimeConnection();
        ProtocolMessageTransponder<ExampleMessage1>? transponder = null;
        transponder = new ProtocolMessageTransponder<ExampleMessage1>(
            new ExampleMessage1(),
            _ => transponder!.Dispose(),
            connection,
            time,
            NullLoggerFactory.Instance
        );
        var completed = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        using var subscription = transponder.State.Subscribe(
            _ => { },
            _ => { },
            _ => completed.TrySetResult()
        );
        transponder.Start(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        time.Advance(TimeSpan.FromSeconds(1));
        await completed.Task.WaitAsync(TestContext.Current.CancellationToken);

        Assert.True(transponder.IsDisposed);
        Assert.Equal(0, connection.SendCount);
        Assert.Equal(0, time.ActiveTimers);
    }
}
