using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using R3;
using Xunit;

namespace Asv.IO.Test;

public class TextStreamLifecycleTest
{
    [Fact]
    public void TelnetStream_CompleteAndEmptyLinesReachPublicReceiveStream()
    {
        using var input = new TestDataStream();
        using var stream = new TelnetStream(input, Encoding.UTF8);
        var received = new List<string>();
        using var subscription = stream.OnReceive.Subscribe(received.Add);

        input.Receive.OnNext(Encoding.UTF8.GetBytes("first\r\n\r\nsecond\r\n"));

        Assert.Equal(new[] { "first", string.Empty, "second" }, received);
    }

    [Fact]
    public void TextStream_NoiseBeforeFrameDoesNotDiscardRemainingInput()
    {
        using var input = new TestDataStream();
        using var stream = new TextStreamBase(input);
        var received = new List<string>();
        using var subscription = stream.OnReceive.Subscribe(received.Add);

        input.Receive.OnNext(Encoding.UTF8.GetBytes("noise\nfirst\rnoise\nsecond\r"));

        Assert.Equal(new[] { "first", "second" }, received);
    }

    [Theory]
    [InlineData(false, "\npayload\r")]
    [InlineData(true, "payload\r\n")]
    public async Task TextStream_SendPreservesWireFraming(bool telnet, string expected)
    {
        using var input = new TestDataStream();
        using ITextStream stream = telnet
            ? new TelnetStream(input, Encoding.UTF8)
            : new TextStreamBase(input);
        byte[]? written = null;
        input.SendHandler = (data, _) =>
        {
            written = data.ToArray();
            return Task.FromResult(true);
        };

        await stream.Send("payload", TestContext.Current.CancellationToken);

        Assert.Equal(Encoding.UTF8.GetBytes(expected), written);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task TextStream_DisposeCancelsPendingSend(bool telnet, bool asyncDispose)
    {
        using var input = new TestDataStream();
        await using ITextStream stream = telnet
            ? new TelnetStream(input, Encoding.UTF8)
            : new TextStreamBase(input);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken observedCancel = default;
        input.SendHandler = async (_, cancel) =>
        {
            observedCancel = cancel;
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancel);
            return true;
        };
        var pending = stream.Send("payload", CancellationToken.None);
        await Wait(started.Task);

        if (asyncDispose)
        {
            await stream.DisposeAsync();
        }
        else
        {
            stream.Dispose();
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Wait(pending));
        Assert.True(observedCancel.IsCancellationRequested);
        stream.Dispose();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task JsonStream_DisposeCancelsRequestWithInfiniteTimeout(
        bool typedRequest,
        bool asyncDispose
    )
    {
        using var text = new TestTextStream();
        await using var json = new JsonStreamBase(text, false, Timeout.InfiniteTimeSpan);
        var pending = typedRequest
            ? json.Request(new { request = 1 }, _ => true, CancellationToken.None)
            : json.RequestText("{}", _ => true, CancellationToken.None);
        Assert.False(pending.IsCompleted);

        if (asyncDispose)
        {
            await json.DisposeAsync();
        }
        else
        {
            json.Dispose();
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Wait(pending));
        Assert.False(text.IsDisposed);
        json.Dispose();
    }

    [Fact]
    public async Task TextRequest_DisposingStreamCancelsRequestWithInfiniteTimeout()
    {
        using var input = new TestDataStream();
        using var text = new TelnetStream(input, Encoding.UTF8);
        var request = text.RequestText("request", Timeout.Infinite, CancellationToken.None);
        Assert.False(request.IsCompleted);

        text.Dispose();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Wait(request));
    }

    [Fact]
    public async Task TextRequest_ResponseBeforeSendCompletionToleratesCancellation()
    {
        using var text = new TestTextStream();
        using var cancel = new CancellationTokenSource();
        var sent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        text.SendHandler = (_, _) =>
        {
            text.Receive.OnNext("response");
            return sent.Task;
        };
        var request = text.RequestText("request", Timeout.Infinite, cancel.Token);
        try
        {
            cancel.Cancel();
        }
        finally
        {
            sent.TrySetResult();
        }

        Assert.Equal(
            "response",
            await request.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task JsonRequest_ResponseBeforeSendCompletionToleratesCancellation(
        bool typedRequest
    )
    {
        using var text = new TestTextStream();
        using var json = new JsonStreamBase(text, false, Timeout.InfiniteTimeSpan);
        using var cancel = new CancellationTokenSource();
        var sent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        text.SendHandler = (_, _) =>
        {
            text.Receive.OnNext("{\"value\":42}");
            return sent.Task;
        };
        Task<JObject> request = typedRequest
            ? json.Request(new { request = 1 }, _ => true, cancel.Token)
            : json.RequestText("{}", _ => true, cancel.Token);
        try
        {
            cancel.Cancel();
        }
        finally
        {
            sent.TrySetResult();
        }

        var response = await request.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(42, response.Value<int>("value"));
    }

    private static Task Wait(Task task) =>
        task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

    private sealed class TestDataStream : IDataStream, IDisposable
    {
        public Subject<byte[]> Receive { get; } = new();
        public Func<ReadOnlyMemory<byte>, CancellationToken, Task<bool>> SendHandler { get; set; } =
            (_, _) => Task.FromResult(true);
        public Observable<byte[]> OnReceive => Receive;
        public string Name => nameof(TestDataStream);
        public long RxBytes => 0;
        public long TxBytes => 0;

        public Task<bool> Send(byte[] data, int count, CancellationToken cancel) =>
            SendHandler(data.AsMemory(0, count), cancel);

        public Task<bool> Send(ReadOnlyMemory<byte> data, CancellationToken cancel) =>
            SendHandler(data, cancel);

        public void Dispose() => Receive.Dispose();
    }

    private sealed class TestTextStream : ITextStream
    {
        private readonly Subject<Exception> _errors = new();
        public Subject<string> Receive { get; } = new();
        public Func<string, CancellationToken, Task> SendHandler { get; set; } =
            (_, _) => Task.CompletedTask;
        public bool IsDisposed { get; private set; }
        public Observable<string> OnReceive => Receive;
        public Observable<Exception> OnError => _errors;

        public Task Send(string value, CancellationToken cancel) => SendHandler(value, cancel);

        public void Dispose()
        {
            IsDisposed = true;
            Receive.Dispose();
            _errors.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
