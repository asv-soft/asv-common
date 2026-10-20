using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using R3;
using Xunit;
using static Asv.IO.Test.Encoder.EncoderTestSupport;

namespace Asv.IO.Test.Encoder;

public class ProtocolLifetimeTest
{
    [Fact]
    public async Task Receive_WaitPolicy_BoundsParsedMessagesAndPreservesOrder()
    {
        var feature = new LifetimeFeature { BlockReceive = true };
        var parser = new LifetimeParser();
        await using var endpoint = new LifetimeEndpoint(CreateContext(feature: feature), parser);
        var received = new ConcurrentQueue<byte>();
        var completed = NewSignal();
        using var subscription = endpoint.OnRxMessage.Subscribe(message =>
        {
            received.Enqueue(((TestMessage)message).Payload[0]);
            if (received.Count == 100)
            {
                completed.TrySetResult();
            }
        });
        endpoint.Feed(0);
        await Wait(feature.ReceiveStarted.Task);
        for (var i = 1; i < 100; i++)
        {
            endpoint.Feed((byte)i);
        }
        await Wait(parser.FourCreated.Task);
        Assert.Equal(4, parser.Created);
        Assert.Equal(1, feature.ActiveReceives);
        feature.ReleaseReceive.TrySetResult();
        await Wait(completed.Task);
        Assert.Equal(Enumerable.Range(0, 100).Select(x => (byte)x), received);
        Assert.Equal(0u, endpoint.Statistic.DroppedRxMessages);
        Assert.Equal(0u, endpoint.Statistic.RxError);
    }

    [Fact]
    public async Task Receive_ForwardedSlowHandler_PropagatesBackpressureToParser()
    {
        var feature = new LifetimeFeature { BlockReceive = true };
        await using var parent = new PublishingConnection(CreateContext(feature: feature));
        var parser = new LifetimeParser();
        await using var endpoint = new LifetimeEndpoint(CreateContext(), parser);
        using var forwarding = endpoint.SubscribeRx(parent.InternalPublishRxMessageAsync);
        endpoint.Feed(0);
        await Wait(feature.ReceiveStarted.Task);
        for (var i = 1; i < 100; i++)
        {
            endpoint.Feed((byte)i);
        }
        await Wait(parser.FourCreated.Task);
        Assert.Equal(4, parser.Created);
        Assert.Equal(1, feature.ActiveReceives);
        feature.ReleaseReceive.TrySetResult();
    }

    [Fact]
    public async Task LegacySynchronousPublisher_BoundsPendingMessagesAndCountsOverflow()
    {
        var feature = new LifetimeFeature { BlockReceive = true };
        await using var publisher = new PublishingConnection(CreateContext(feature: feature));
        var received = 0;
        var completed = NewSignal();
        using var subscription = publisher.OnRxMessage.Subscribe(_ =>
        {
            if (++received == 101)
            {
                completed.TrySetResult();
            }
        });
        for (var i = 0; i < 102; i++)
        {
            publisher.Publish(new TestMessage((byte)i));
        }
        Assert.Equal(1, feature.ActiveReceives);
        Assert.Equal(1u, publisher.Statistic.DroppedRxMessages);
        feature.ReleaseReceive.TrySetResult();
        await Wait(completed.Task);
        Assert.Equal(101, received);
    }

    [Fact]
    public async Task Receive_DropOldestPolicy_KeepsNewestMessagesAndCountsDrops()
    {
        var feature = new LifetimeFeature { BlockReceive = true };
        var parser = new LifetimeParser();
        await using var endpoint = new LifetimeEndpoint(
            CreateContext(feature: feature),
            parser,
            drop: true
        );
        var received = new ConcurrentQueue<byte>();
        var completed = NewSignal();
        using var subscription = endpoint.OnRxMessage.Subscribe(message =>
        {
            received.Enqueue(((TestMessage)message).Payload[0]);
            if (received.Count == 3)
            {
                completed.TrySetResult();
            }
        });
        endpoint.Feed(0);
        await Wait(feature.ReceiveStarted.Task);
        for (var i = 1; i < 100; i++)
        {
            endpoint.Feed((byte)i);
        }
        await Wait(parser.HundredCreated.Task);
        feature.ReleaseReceive.TrySetResult();
        await Wait(completed.Task);
        Assert.Equal(new byte[] { 0, 98, 99 }, received);
        Assert.Equal(97u, endpoint.Statistic.DroppedRxMessages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dispose_CancelsTransportAndQueuedSenderAndReleasesOwnedResources(
        bool asynchronous
    )
    {
        var feature = new LifetimeFeature();
        var parser = new LifetimeParser();
        var endpoint = new LifetimeEndpoint(CreateContext(feature: feature), parser)
        {
            BlockWrite = true,
        };
        await endpoint.Send(new TestMessage(1), TestContext.Current.CancellationToken);
        await Wait(endpoint.WriteStarted.Task);
        await endpoint.Send(new TestMessage(2), TestContext.Current.CancellationToken);
        var waiting = endpoint
            .Send(new TestMessage(3), TestContext.Current.CancellationToken)
            .AsTask();
        Assert.False(waiting.IsCompleted);
        if (asynchronous)
        {
            await Wait(endpoint.DisposeAsync().AsTask());
        }
        else
        {
            endpoint.Dispose();
            await Wait(endpoint.Shutdown);
        }
        var exception = await Record.ExceptionAsync(() => Wait(waiting));
        Assert.True(
            exception is ChannelClosedException or OperationCanceledException,
            exception?.ToString()
        );
        Assert.True(endpoint.WriteCancellation.IsCancellationRequested);
        Assert.Equal(1, parser.DisposeCalls);
        Assert.Equal(1, feature.UnregisterCalls);
        Assert.Equal(0u, endpoint.Statistic.TxError);
        endpoint.Dispose();
        Assert.Equal(1, parser.DisposeCalls);
    }

    [Fact]
    public async Task Dispose_AbandonsFeatureThatIgnoresCancellationWithoutPublishing()
    {
        var feature = new LifetimeFeature { BlockReceive = true };
        var parser = new LifetimeParser();
        var endpoint = new LifetimeEndpoint(CreateContext(feature: feature), parser);
        var received = 0;
        using var subscription = endpoint.OnRxMessage.Subscribe(_ => received++);
        endpoint.Feed(1);
        await Wait(feature.ReceiveStarted.Task);
        try
        {
            await Wait(endpoint.DisposeAsync().AsTask());
            Assert.Equal(1, parser.DisposeCalls);
            Assert.Equal(1, feature.UnregisterCalls);
            Assert.Equal(0, received);
        }
        finally
        {
            feature.ReleaseReceive.TrySetResult();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VirtualPort_DisposalAndSemaphoreGrant_CancelEveryWaitingSend(
        bool cancellableCaller
    )
    {
        using var caller = new CancellationTokenSource();
        var cancel = cancellableCaller ? caller.Token : CancellationToken.None;
        for (var iteration = 0; iteration < 32; iteration++)
        {
            var entered = NewSignal();
            var release = NewSignal();
            var feature = new TestFeature(async message =>
            {
                entered.TrySetResult();
                await release.Task;
                return message;
            });
            await using var port = new VirtualPort(
                "grant",
                CreateContext(feature: feature),
                _ => true,
                new Statistic()
            );
            var frames = 0;
            using var subscription = port.Tx.Subscribe(_ => Interlocked.Increment(ref frames));
            var sends = new Task[9];
            sends[0] = port.Send(new TestMessage(0), cancel).AsTask();
            await Wait(entered.Task);
            for (var i = 1; i < sends.Length; i++)
            {
                sends[i] = port.Send(new TestMessage((byte)i), cancel).AsTask();
                Assert.False(sends[i].IsCompleted);
            }
            try
            {
                await port.DisposeAsync();
                foreach (var send in sends)
                {
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Wait(send));
                }
                Assert.Equal(0, frames);
                Assert.False(caller.IsCancellationRequested);
            }
            finally
            {
                release.TrySetResult();
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VirtualPort_DisposesOwnedParserExactlyOnce(bool asynchronous)
    {
        var parser = new LifetimeParser();
        var factory = Protocol.Create(builder =>
            builder.Protocols.Register(Info, (_, _) => parser)
        );
        var port = new VirtualPort(
            "virtual",
            (IProtocolContext)factory,
            _ => true,
            new Statistic()
        );
        if (asynchronous)
        {
            await port.DisposeAsync();
        }
        else
        {
            port.Dispose();
        }
        port.Dispose();
        Assert.Equal(1, parser.DisposeCalls);
    }

    [Fact]
    public void VirtualPort_ReentrantDispose_DoesNotPushOrResetDisposedParser()
    {
        var parser = new LifetimeParser();
        var factory = Protocol.Create(builder =>
            builder.Protocols.Register(Info, (_, _) => parser)
        );
        var port = new VirtualPort(
            "virtual",
            (IProtocolContext)factory,
            _ => true,
            new Statistic()
        );
        using var subscription = port.OnRxMessage.Subscribe(_ => port.Dispose());
        port.Rx.OnNext([1, 2, 3]);
        Assert.Equal(1, parser.Created);
        Assert.Equal(1, parser.DisposeCalls);
    }

    [Fact]
    public void QueueConfiguration_SeparatesRxAndTxAndPreservesLegacyFallback()
    {
        var config = new ProtocolPortConfig(new Uri("test://localhost?tx_queue=8"));
        Assert.Equal(8, config.RxQueueSize);
        config.RxQueueSize = 2;
        Assert.Equal(8, config.TxQueueSize);
        Assert.Equal(2, config.RxQueueSize);
        config.RxQueueSize = 0;
        Assert.Equal(8, config.TxQueueSize);
        Assert.Equal(0, config.RxQueueSize);
        var roundTrip = new ProtocolPortConfig(config.AsUri());
        Assert.Equal(8, roundTrip.TxQueueSize);
        Assert.Equal(0, roundTrip.RxQueueSize);
        Assert.Equal(100, new ProtocolPortConfig(new Uri("test://localhost")).RxQueueSize);
    }

    [Fact]
    public void DefaultEncoder_DoesNotRetainUnknownProtocolId()
    {
        var encoders = new ProtocolEncoderSet(CreateContext());
        var weak = GetUnknownEncoder(encoders);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(weak.IsAlive);
        GC.KeepAlive(encoders);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference GetUnknownEncoder(ProtocolEncoderSet encoders)
    {
        var id = Guid.NewGuid().ToString();
        var weak = new WeakReference(id);
        Assert.Same(DefaultProtocolEncoder.Instance, encoders.Get(id));
        return weak;
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class PublishingConnection(IProtocolContext context)
        : ProtocolConnection("publisher", context)
    {
        public void Publish(IProtocolMessage message) => InternalPublishRxMessage(message);

        public override ValueTask Send(
            IProtocolMessage message,
            CancellationToken cancel = default
        ) => ValueTask.CompletedTask;
    }

    private sealed class LifetimeEndpoint : ProtocolEndpoint
    {
        private readonly Channel<byte> _input = Channel.CreateUnbounded<byte>();

        public LifetimeEndpoint(IProtocolContext context, IProtocolParser parser, bool drop = false)
            : base(
                "lifetime",
                new ProtocolPortConfig(new Uri("test://localhost"))
                {
                    RxQueueSize = 2,
                    TxQueueSize = 1,
                    DropMessageWhenFullRxQueue = drop,
                    DropMessageWhenFullTxQueue = false,
                },
                [parser],
                context,
                null,
                false
            )
        {
            Start();
        }

        public bool BlockWrite { get; init; }
        public TaskCompletionSource WriteStarted { get; } = NewSignal();
        public CancellationToken WriteCancellation { get; private set; }

        public void Feed(byte value) => _input.Writer.TryWrite(value);

        protected override int GetAvailableBytesToRead() => 1;

        protected override async ValueTask<int> InternalRead(
            Memory<byte> memory,
            CancellationToken cancel
        )
        {
            var value = await _input.Reader.ReadAsync(cancel);
            memory.Span[0] = value;
            return 1;
        }

        protected override async ValueTask<int> InternalWrite(
            ReadOnlyMemory<byte> memory,
            CancellationToken cancel
        )
        {
            WriteCancellation = cancel;
            WriteStarted.TrySetResult();
            if (BlockWrite)
            {
                await Task.Delay(Timeout.Infinite, cancel);
            }
            return memory.Length;
        }
    }

    private sealed class LifetimeParser : IProtocolParser
    {
        private readonly Subject<IProtocolMessage> _messages = new();
        private readonly Subject<Exception> _errors = new();
        private int _created;
        public int Created => Volatile.Read(ref _created);
        public int DisposeCalls { get; private set; }
        public TaskCompletionSource FourCreated { get; } = NewSignal();
        public TaskCompletionSource HundredCreated { get; } = NewSignal();
        public IStatistic Statistic { get; } = new Statistic();
        public ProtocolInfo Info => EncoderTestSupport.Info;
        public ProtocolTags Tags { get; } = [];
        public Observable<IProtocolMessage> OnMessage => _messages;
        public Observable<Exception> OnError => _errors;

        public bool Push(byte data)
        {
            Assert.Equal(0, DisposeCalls);
            _messages.OnNext(new TestMessage(data));
            var count = Interlocked.Increment(ref _created);
            if (count == 4)
            {
                FourCreated.TrySetResult();
            }
            if (count == 100)
            {
                HundredCreated.TrySetResult();
            }
            return true;
        }

        public void Reset() => Assert.Equal(0, DisposeCalls);

        public void Dispose()
        {
            DisposeCalls++;
            _messages.Dispose();
            _errors.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class LifetimeFeature : IProtocolFeature
    {
        private int _activeReceives;
        public bool BlockReceive { get; init; }
        public int ActiveReceives => Volatile.Read(ref _activeReceives);
        public int UnregisterCalls { get; private set; }
        public TaskCompletionSource ReceiveStarted { get; } = NewSignal();
        public TaskCompletionSource ReleaseReceive { get; } = NewSignal();
        public string Id => "lifetime";
        public string Name => Id;
        public string Description => Id;
        public int Priority => 0;

        public void Register(IProtocolConnection connection) { }

        public void Unregister(IProtocolConnection connection) => UnregisterCalls++;

        public ValueTask<IProtocolMessage?> ProcessTx(
            IProtocolMessage message,
            IProtocolConnection connection,
            CancellationToken cancel
        ) => ValueTask.FromResult<IProtocolMessage?>(message);

        public async ValueTask<IProtocolMessage?> ProcessRx(
            IProtocolMessage message,
            IProtocolConnection connection,
            CancellationToken cancel
        )
        {
            Interlocked.Increment(ref _activeReceives);
            try
            {
                ReceiveStarted.TrySetResult();
                if (BlockReceive)
                {
                    await ReleaseReceive.Task;
                }
                return message;
            }
            finally
            {
                Interlocked.Decrement(ref _activeReceives);
            }
        }
    }
}
