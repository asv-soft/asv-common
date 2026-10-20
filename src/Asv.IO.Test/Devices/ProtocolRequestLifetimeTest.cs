using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Asv.IO.Test;

public class ProtocolRequestLifetimeTest
{
    [Fact]
    public async Task SendAndWaitAnswer_TimeoutCancelsPendingResponse()
    {
        var time = new DeviceLifetimeTimeProvider();
        using var connection = new DeviceLifetimeConnection();
        var request = connection.SendAndWaitAnswer<int, ExampleMessage1, byte>(
            new ExampleMessage1(),
            GetValue,
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken,
            time
        );

        Assert.False(request.IsCompleted);
        time.Advance(TimeSpan.FromSeconds(5));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        Assert.Equal(1, connection.SendCount);
        Assert.Equal(0, time.ActiveTimers);
    }

    [Fact]
    public async Task SendAndWaitAnswer_EarlyResponseDisposesProviderTimer()
    {
        var time = new DeviceLifetimeTimeProvider();
        using var connection = new DeviceLifetimeConnection();
        var request = connection.SendAndWaitAnswer<int, ExampleMessage1, byte>(
            new ExampleMessage1(),
            GetValue,
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken,
            time
        );
        connection.Receive(new ExampleMessage1 { Value1 = 42 });

        Assert.Equal(42, await request);
        Assert.Equal(0, time.ActiveTimers);
        time.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(1, connection.SendCount);
    }

    [Fact]
    public async Task SendAndWaitAnswer_RetryLimitAboveByteRangeTerminatesWithTimeout()
    {
        var time = new DeviceLifetimeTimeProvider();
        using var connection = new DeviceLifetimeConnection
        {
            SendCallback = (_, _) => ValueTask.FromException(new OperationCanceledException()),
        };

        await Assert.ThrowsAsync<TimeoutException>(() =>
            connection.SendAndWaitAnswer<int, ExampleMessage1, ExampleMessage1, byte>(
                new ExampleMessage1(),
                GetValue,
                TimeSpan.FromSeconds(5),
                256,
                cancel: TestContext.Current.CancellationToken,
                timeProvider: time
            )
        );

        Assert.Equal(256, connection.SendCount);
        Assert.Equal(0, time.ActiveTimers);
    }

    [Fact]
    public async Task SendAndWaitAnswer_ExternalCancellationDoesNotRetry()
    {
        var time = new DeviceLifetimeTimeProvider();
        using var cancel = new CancellationTokenSource();
        using var connection = new DeviceLifetimeConnection
        {
            SendCallback = (_, token) =>
            {
                cancel.Cancel();
                return ValueTask.FromCanceled(token);
            },
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            connection.SendAndWaitAnswer<int, ExampleMessage1, ExampleMessage1, byte>(
                new ExampleMessage1(),
                GetValue,
                TimeSpan.FromSeconds(5),
                3,
                cancel: cancel.Token,
                timeProvider: time
            )
        );

        Assert.Equal(1, connection.SendCount);
        Assert.Equal(0, time.ActiveTimers);
    }

    [Fact]
    public async Task MicroserviceRequest_DisposalDoesNotRetryOrLeaveTimer()
    {
        var time = new DeviceLifetimeTimeProvider();
        using var connection = new DeviceLifetimeConnection();
        var service = new RequestService(new MicroserviceContext(connection, timeProvider: time));
        var request = service.RequestWithRetries();
        Assert.Equal(1, connection.SendCount);

        service.Dispose();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        Assert.Equal(1, connection.SendCount);
        Assert.Equal(0, time.ActiveTimers);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MicroserviceRequest_EarlyResponseReleasesTimeoutForBothOverloads(
        bool packetAnswer
    )
    {
        var time = new DeviceLifetimeTimeProvider();
        using var connection = new DeviceLifetimeConnection();
        using var service = new RequestService(
            new MicroserviceContext(connection, timeProvider: time)
        );
        var request = service.Request(packetAnswer);
        connection.Receive(new ExampleMessage1 { Value1 = 17 });

        Assert.Equal(17, await request);
        Assert.Equal(0, time.ActiveTimers);
        time.Advance(TimeSpan.FromSeconds(10));
    }

    private static bool GetValue(ExampleMessage1 message, out int result)
    {
        result = message.Value1;
        return true;
    }

    private sealed class RequestService(IMicroserviceContext context)
        : MicroserviceClient<ExampleMessageBase>(context, "request-test")
    {
        public override string TypeName => "request-test";

        protected override void FillMessageBeforeSent(ExampleMessageBase message) { }

        protected override bool FilterDeviceMessages(ExampleMessageBase arg) => true;

        public Task<int> RequestWithRetries() =>
            InternalCall<int, ExampleMessage1, ExampleMessage1>(
                new ExampleMessage1(),
                GetValue,
                attemptCount: 3,
                timeoutMs: 5000
            );

        public async Task<int> Request(bool packetAnswer)
        {
            if (packetAnswer)
            {
                var answer = await InternalSendAndWaitAnswer<ExampleMessage1>(
                    new ExampleMessage1(),
                    CancellationToken.None,
                    timeoutMs: 5000
                );
                return answer.Value1;
            }
            return await InternalSendAndWaitAnswer<int, ExampleMessage1>(
                new ExampleMessage1(),
                GetValue,
                5000
            );
        }
    }
}
