using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using R3;
using Xunit;

namespace Asv.IO.Test;

public class ConcreteTransportLifecycleTest
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void SerialEndpoint_FailedInitializationDisposesItsParsers(
        bool alternative,
        bool invalidConfiguration
    )
    {
        var context = CreateContext();
        var parser = new TrackingParser();
        var config = new SerialProtocolPortConfig(new Uri("serial:///__asv_io_missing__/port"));
        if (invalidConfiguration)
        {
            config.WriteTimeout = 0;
        }

        Assert.ThrowsAny<Exception>(() =>
        {
            if (alternative)
            {
                using var endpoint = new AlternativeSerialProtocolEndpoint(
                    "missing",
                    config,
                    [parser],
                    context,
                    new Statistic()
                );
            }
            else
            {
                using var endpoint = new SerialProtocolEndpoint(
                    "missing",
                    config,
                    [parser],
                    context,
                    new Statistic()
                );
            }
        });

        Assert.Equal(1, parser.Disposals);
        Assert.Empty(parser.Received);
    }

    [Fact]
    public async Task UdpEndpoint_EmptyDatagramDoesNotBlockFollowingData()
    {
        using var socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp
        );
        var parser = new TrackingParser(expectedBytes: 3);
        await using var endpoint = new UdpSocketProtocolEndpoint(
            socket,
            new IPEndPoint(IPAddress.Loopback, 12345),
            "udp",
            UdpConfig(),
            [parser],
            CreateContext(),
            new Statistic()
        );

        await Wait(Task.Run(() => endpoint.ApplyData([], 0)));
        await Wait(Task.Run(() => endpoint.ApplyData(new byte[] { 1, 2, 3 }, 3)));
        await Wait(parser.Completed.Task);

        Assert.Equal(new byte[] { 1, 2, 3 }, parser.Received.ToArray());
        Assert.Equal(3U, endpoint.Statistic.RxBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UdpEndpoint_DisposeReleasesProducerWaitingForRead(bool asyncDispose)
    {
        using var socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp
        );
        var parser = new TrackingParser();
        await using var endpoint = new PausedUdpEndpoint(socket, parser, CreateContext());
        var producer = Task.Run(() => endpoint.ApplyData(new byte[] { 1 }, 1));
        await Wait(endpoint.ReadStarted.Task);
        Assert.False(producer.IsCompleted);

        if (asyncDispose)
        {
            await endpoint.DisposeAsync();
        }
        else
        {
            endpoint.Dispose();
        }
        await Wait(producer);
        await Wait(parser.Disposed.Task);

        Assert.Equal(1, parser.Disposals);
        Assert.Empty(parser.Received);
    }

    [Fact]
    public async Task TcpServerProtocolPort_AcceptedPeersHaveDistinctRemoteIdentifiers()
    {
        var config = new TcpServerProtocolPortConfig(new Uri("tcps://127.0.0.1:0"))
        {
            ReconnectTimeoutMs = 60_000,
        };
        await using var server = new TcpServerProtocolPort(
            config,
            CreateContext(),
            new Statistic()
        );
        var endpoints = Channel.CreateUnbounded<IProtocolEndpoint>();
        using var subscription = server.EndpointAdded.Subscribe(endpoint =>
            endpoints.Writer.TryWrite(endpoint)
        );
        var connected = server.Status.FirstAsync(status => status == ProtocolPortStatus.Connected);
        server.Enable();
        await Wait(connected);
        var portNumber = Assert.IsType<IPEndPoint>(server.LocalEndPoint).Port;
        using var firstClient = new TcpClient();
        using var secondClient = new TcpClient();
        await firstClient.ConnectAsync(
            IPAddress.Loopback,
            portNumber,
            TestContext.Current.CancellationToken
        );
        await secondClient.ConnectAsync(
            IPAddress.Loopback,
            portNumber,
            TestContext.Current.CancellationToken
        );
        var first = await endpoints
            .Reader.ReadAsync(TestContext.Current.CancellationToken)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5));
        var second = await endpoints
            .Reader.ReadAsync(TestContext.Current.CancellationToken)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(ExpectedId(first), first.Id);
        Assert.Equal(ExpectedId(second), second.Id);

        string ExpectedId(IProtocolEndpoint endpoint) =>
            ProtocolHelper.NormalizeId(
                $"{server.Id}_{Assert.IsAssignableFrom<TcpSocketProtocolEndpoint>(endpoint).Socket.RemoteEndPoint}"
            );
    }

    private static ProtocolPortConfig UdpConfig() =>
        new(new Uri("udp://127.0.0.1:0")) { ReconnectTimeoutMs = 60_000 };

    private static IProtocolContext CreateContext() => (IProtocolContext)Protocol.Create(_ => { });

    private static Task Wait(Task task) =>
        task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

    private sealed class PausedUdpEndpoint(
        Socket socket,
        TrackingParser parser,
        IProtocolContext context
    )
        : UdpSocketProtocolEndpoint(
            socket,
            new IPEndPoint(IPAddress.Loopback, 12345),
            "paused",
            UdpConfig(),
            [parser],
            context,
            new Statistic()
        )
    {
        public TaskCompletionSource ReadStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async ValueTask<int> InternalRead(
            Memory<byte> memory,
            CancellationToken cancel
        )
        {
            ReadStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancel);
            return await base.InternalRead(memory, cancel);
        }
    }

    private sealed class TrackingParser(int expectedBytes = 0) : IProtocolParser
    {
        private readonly Subject<IProtocolMessage> _messages = new();
        private readonly Subject<Exception> _errors = new();
        private int _disposals;
        public ConcurrentQueue<byte> Received { get; } = new();
        public TaskCompletionSource Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Disposed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Disposals => Volatile.Read(ref _disposals);
        public ProtocolInfo Info { get; } = new("transport-test", "Transport test");
        public IStatistic Statistic { get; } = new Statistic();
        public ProtocolTags Tags { get; } = [];
        public Observable<IProtocolMessage> OnMessage => _messages;
        public Observable<Exception> OnError => _errors;

        public bool Push(byte data)
        {
            Received.Enqueue(data);
            if (Received.Count == expectedBytes)
            {
                Completed.TrySetResult();
            }
            return false;
        }

        public void Reset() { }

        public void Dispose()
        {
            Interlocked.Increment(ref _disposals);
            _messages.Dispose();
            _errors.Dispose();
            Disposed.TrySetResult();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
