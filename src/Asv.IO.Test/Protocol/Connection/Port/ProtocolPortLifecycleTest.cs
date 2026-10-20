using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using R3;
using Xunit;

namespace Asv.IO.Test;

public class ProtocolPortLifecycleTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dispose_BlockedConnectCancelsAndCleansLateResource(bool asyncDispose)
    {
        using var release = new ManualResetEventSlim();
        var entered = Completion();
        var cancelled = Completion();
        using var port = new TestPort(CreateContext());
        var connected = 0;
        using var status = port.Status.Subscribe(value =>
        {
            if (value == ProtocolPortStatus.Connected)
            {
                Interlocked.Increment(ref connected);
            }
        });
        port.EnableAction = token =>
        {
            using var registration = token.Register(() => cancelled.TrySetResult());
            entered.TrySetResult();
            release.Wait(TestContext.Current.CancellationToken);
            port.InstallResource();
        };
        port.Enable();
        Assert.True(port.IsEnabled.CurrentValue);
        Assert.True(port.Config.IsEnabled);
        await Wait(entered.Task);

        Task disposal;
        if (asyncDispose)
        {
            disposal = port.DisposeAsync().AsTask();
        }
        else
        {
            port.Dispose();
            disposal = Task.CompletedTask;
        }
        try
        {
            await Wait(cancelled.Task);
            if (asyncDispose)
            {
                Assert.False(disposal.IsCompleted);
            }
        }
        finally
        {
            release.Set();
        }
        await Wait(port.ResourceReleased.Task);
        await Wait(disposal);
        Assert.False(port.HasResource);
        Assert.Empty(port.Endpoints);
        Assert.Equal(0, connected);
    }

    [Fact]
    public async Task DisableThenEnable_WaitsForPreviousConnectBeforeInstallingNextGeneration()
    {
        using var release = new ManualResetEventSlim();
        var entered = Completion();
        CancellationToken firstToken = default;
        await using var port = new TestPort(CreateContext());
        port.EnableAction = token =>
        {
            if (port.EnableCalls == 1)
            {
                firstToken = token;
                entered.TrySetResult();
                release.Wait(TestContext.Current.CancellationToken);
            }
            port.InstallResource();
        };
        var nextConnection = WhenConnected(port);
        port.Enable();
        await Wait(entered.Task);
        try
        {
            port.Disable();
            Assert.False(port.IsEnabled.CurrentValue);
            Assert.False(port.Config.IsEnabled);
            Assert.True(firstToken.IsCancellationRequested);
            port.Enable();
            Assert.True(port.IsEnabled.CurrentValue);
            Assert.Equal(1, port.EnableCalls);
        }
        finally
        {
            release.Set();
        }
        await Wait(nextConnection);
        Assert.Equal(2, port.EnableCalls);
        Assert.True(port.HasResource);
        Assert.True(port.ResourceReleased.Task.IsCompleted);
    }

    [Fact]
    public async Task AddEndpoint_PreviousGenerationIsRejectedAfterReenable()
    {
        var context = CreateContext();
        await using var port = new TestPort(context);
        CancellationToken generation = default;
        port.EnableAction = token => generation = token;
        var initialConnection = WhenConnected(port);
        port.Enable();
        await Wait(initialConnection);
        var previous = generation;
        var disconnected = WhenDisconnected(port);
        port.Disable();
        await Wait(disconnected);
        var nextConnection = WhenConnected(port);
        port.Enable();
        await Wait(nextConnection);
        var current = new TestEndpoint(context);
        port.Attach(current, generation);
        var stale = new TestEndpoint(context);

        port.Attach(stale, previous);

        Assert.True(stale.IsDisposed);
        Assert.False(current.IsDisposed);
        Assert.Same(current, Assert.Single(port.Endpoints));
    }

    [Fact]
    public async Task Dispose_FromInProgressObserverPreventsTransportCreation()
    {
        using var port = new TestPort(CreateContext());
        using var subscription = port.Status.Subscribe(status =>
        {
            if (status == ProtocolPortStatus.InProgress)
            {
                port.Dispose();
            }
        });
        port.Enable();
        await Wait(port.Disposed.Task);
        Assert.Equal(0, port.EnableCalls);
        Assert.Empty(port.Endpoints);
    }

    [Fact]
    public async Task Dispose_FromEndpointAddedObserverReleasesEndpointAndFeatureRegistration()
    {
        var feature = new TrackingFeature();
        var context = CreateContext(feature: feature);
        using var port = new TestPort(context);
        TestEndpoint? endpoint = null;
        port.EnableAction = _ =>
        {
            endpoint = new TestEndpoint(context);
            port.Attach(endpoint);
        };
        using var subscription = port.EndpointAdded.Subscribe(_ => port.Dispose());
        port.Enable();
        await Wait(port.Disposed.Task);
        Assert.NotNull(endpoint);
        Assert.True(endpoint.IsDisposed);
        Assert.Empty(port.Endpoints);
        Assert.Equal(2, feature.Registrations);
        Assert.Equal(2, feature.Unregistrations);
    }

    [Fact]
    public async Task ErrorObserver_DisposesPortBeforeReconnectTimerIsCreated()
    {
        var time = new DeviceLifetimeTimeProvider();
        using var port = new TestPort(CreateContext(time));
        var initialConnection = WhenConnected(port);
        port.Enable();
        await Wait(initialConnection);
        using var subscription = port.Error.Subscribe(error =>
        {
            if (error != null)
            {
                port.Dispose();
            }
        });

        port.RaiseError();

        Assert.True(port.IsDisposed);
        Assert.Empty(time.Timers);
    }

    [Fact]
    public async Task Reconnect_UsesProviderAndDisableReleasesPendingTimer()
    {
        var time = new DeviceLifetimeTimeProvider();
        await using var port = new TestPort(CreateContext(time));
        var initialConnection = WhenConnected(port);
        port.Enable();
        await Wait(initialConnection);
        port.RaiseError();
        Assert.Equal(1, time.ActiveTimers);
        var connected = WhenConnected(port);

        time.Advance(TimeSpan.FromMilliseconds(99));
        Assert.False(connected.IsCompleted);
        Assert.Equal(1, port.EnableCalls);
        time.Advance(TimeSpan.FromMilliseconds(1));
        await Wait(connected);
        Assert.Equal(2, port.EnableCalls);
        Assert.Equal(0, time.ActiveTimers);

        port.RaiseError();
        Assert.Equal(1, time.ActiveTimers);
        var disconnected = WhenDisconnected(port);
        port.Disable();
        Assert.Equal(0, time.ActiveTimers);
        await Wait(disconnected);
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(2, port.EnableCalls);
    }

    [Fact]
    public async Task Reconnect_InvalidDelayDisposesCreatedTimer()
    {
        var time = new DeviceLifetimeTimeProvider();
        await using var port = new TestPort(CreateContext(time), reconnectTimeout: -2);
        var connected = WhenConnected(port);
        port.Enable();
        await Wait(connected);

        port.RaiseError();

        Assert.True(Assert.Single(time.Timers).IsDisposed);
        Assert.Equal(0, time.ActiveTimers);
        Assert.Equal(ProtocolPortStatus.Error, port.Status.CurrentValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RouterDispose_RejectsAndDisposesLateFactoryResult(bool asyncDispose)
    {
        using var release = new ManualResetEventSlim();
        var entered = Completion();
        TestPort? created = null;
        var feature = new TrackingFeature();
        var factory = Protocol.Create(builder =>
        {
            builder.Features.Register(feature);
            builder.PortTypes.Register(
                TestPort.Info,
                (_, context, _) =>
                {
                    created = new TestPort(context);
                    entered.TrySetResult();
                    release.Wait(TestContext.Current.CancellationToken);
                    return created;
                }
            );
        });
        using var router = factory.CreateRouter("router");
        var addition = Task.Run(() => router.AddPort(new Uri("test://localhost")));
        await Wait(entered.Task);
        try
        {
            if (asyncDispose)
            {
                await router.DisposeAsync();
            }
            else
            {
                router.Dispose();
            }
        }
        finally
        {
            release.Set();
        }
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await Wait(addition));
        Assert.NotNull(created);
        Assert.True(created.IsDisposed);
        Assert.Equal(0, created.EnableCalls);
        Assert.Empty(router.Ports);
        Assert.Equal(2, feature.Registrations);
        Assert.Equal(2, feature.Unregistrations);
    }

    [Fact]
    public void RouterDispose_FromPortUpdatedObserverDisposesAlreadyOwnedPort()
    {
        TestPort? created = null;
        var factory = Protocol.Create(builder =>
            builder.PortTypes.Register(
                TestPort.Info,
                (_, context, _) =>
                {
                    created = new TestPort(context);
                    return created;
                }
            )
        );
        using var router = factory.CreateRouter("router");
        using var subscription = router.PortUpdated.Subscribe(_ => router.Dispose());

        Assert.Throws<ObjectDisposedException>(() => router.AddPort(new Uri("test://localhost")));

        Assert.NotNull(created);
        Assert.True(created.IsDisposed);
        Assert.Empty(router.Ports);
        Assert.Equal(0, created.EnableCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Send_ForwardsFeatureReplacementToOwnedConnection(bool routerLevel)
    {
        var replacement = new ExampleMessage1();
        var feature = new TrackingFeature
        {
            Replacement = replacement,
            ReplaceAtRouter = routerLevel,
        };
        TestEndpoint? endpoint = null;
        var factory = Protocol.Create(builder =>
        {
            builder.Features.Register(feature);
            builder.PortTypes.Register(
                TestPort.Info,
                (_, context, _) =>
                {
                    var port = new TestPort(context);
                    endpoint = new TestEndpoint(context);
                    port.Attach(endpoint);
                    return port;
                }
            );
        });
        await using var router = factory.CreateRouter("router");
        var port = router.AddPort(new Uri("test://localhost"));
        var message = new ExampleMessage1();

        await (routerLevel ? router : (IProtocolConnection)port).Send(
            message,
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(endpoint);
        Assert.Same(replacement, endpoint.SentMessage);
    }

    [Fact]
    public void CreateParsers_LaterFactoryFailureDisposesEarlierParser()
    {
        var parser = new TestParser();
        var factory = Protocol.Create(builder =>
        {
            builder.Protocols.Register(parser.Info, (_, _) => parser);
            builder.Protocols.Register(
                new ProtocolInfo("failing", "Failing"),
                (_, _) => throw new InvalidOperationException()
            );
        });
        using var port = new TestPort((IProtocolContext)factory);

        Assert.Throws<InvalidOperationException>(port.CreateParsers);

        Assert.Equal(1, parser.Disposals);
    }

    private static IProtocolContext CreateContext(
        TimeProvider? time = null,
        TrackingFeature? feature = null
    ) =>
        (IProtocolContext)
            Protocol.Create(builder =>
            {
                if (time != null)
                {
                    builder.SetTimeProvider(time);
                }
                if (feature != null)
                {
                    builder.Features.Register(feature);
                }
            });

    private static Task WhenConnected(TestPort port) =>
        WhenStatus(port, ProtocolPortStatus.Connected);

    private static Task WhenDisconnected(TestPort port) =>
        WhenStatus(port, ProtocolPortStatus.Disconnected);

    private static async Task WhenStatus(TestPort port, ProtocolPortStatus expected)
    {
        // FirstAsync can resume inside OnNext and let the next subscription receive
        // the same in-flight notification. Keep test continuations asynchronous.
        var completion = Completion();
        using var subscription = port.Status.Subscribe(value =>
        {
            if (value == expected)
            {
                completion.TrySetResult();
            }
        });
        await Wait(completion.Task).ConfigureAwait(false);
    }

    private static TaskCompletionSource Completion() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static Task Wait(Task task) =>
        task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

    private sealed class TestPort(IProtocolContext context, int reconnectTimeout = 100)
        : ProtocolPort<ProtocolPortConfig>(
            "test-port",
            new ProtocolPortConfig(new Uri("test://localhost?enabled=false"))
            {
                ReconnectTimeoutMs = reconnectTimeout,
            },
            context,
            true
        )
    {
        private int _resource;
        private int _enableCalls;
        public static readonly PortTypeInfo Info = new("test", "Test");
        public override PortTypeInfo TypeInfo => Info;
        public Action<CancellationToken>? EnableAction { get; set; }
        public int EnableCalls => Volatile.Read(ref _enableCalls);
        public bool HasResource => Volatile.Read(ref _resource) != 0;
        public TaskCompletionSource ResourceReleased { get; } = Completion();
        public TaskCompletionSource Disposed { get; } = Completion();

        public void InstallResource() => Interlocked.Exchange(ref _resource, 1);

        public void Attach(IProtocolEndpoint endpoint) => InternalAddEndpoint(endpoint);

        public void Attach(IProtocolEndpoint endpoint, CancellationToken token) =>
            InternalAddEndpoint(endpoint, token);

        public void RaiseError() =>
            InternalRisePortErrorAndReconnect(new InvalidOperationException("test error"));

        public void CreateParsers() => InternalCreateParsers();

        protected override void InternalSafeEnable(CancellationToken token)
        {
            Interlocked.Increment(ref _enableCalls);
            EnableAction?.Invoke(token);
        }

        protected override void InternalSafeDisable()
        {
            if (Interlocked.Exchange(ref _resource, 0) != 0)
            {
                ResourceReleased.TrySetResult();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Disposed.TrySetResult();
        }
    }

    private sealed class TestEndpoint(IProtocolContext context)
        : ProtocolConnection("test-endpoint", context),
            IProtocolEndpoint
    {
        private readonly ReactiveProperty<ProtocolConnectionException?> _error = new();
        public IEnumerable<IProtocolParser> Parsers => [];
        public ReadOnlyReactiveProperty<ProtocolConnectionException?> LastError => _error;
        public IProtocolMessage? SentMessage { get; private set; }

        public override ValueTask Send(IProtocolMessage message, CancellationToken cancel = default)
        {
            SentMessage = message;
            return ValueTask.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            _error.Dispose();
            base.Dispose(disposing);
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            _error.Dispose();
            await base.DisposeAsyncCore();
        }
    }

    private sealed class TrackingFeature : IProtocolFeature
    {
        private int _registrations;
        private int _unregistrations;
        public string Name => "lifetime";
        public string Description => Name;
        public string Id => Name;
        public int Priority => 0;
        public int Registrations => Volatile.Read(ref _registrations);
        public int Unregistrations => Volatile.Read(ref _unregistrations);
        public IProtocolMessage? Replacement { get; init; }
        public bool ReplaceAtRouter { get; init; }

        public void Register(IProtocolConnection connection) =>
            Interlocked.Increment(ref _registrations);

        public void Unregister(IProtocolConnection connection) =>
            Interlocked.Increment(ref _unregistrations);

        public ValueTask<IProtocolMessage?> ProcessRx(
            IProtocolMessage message,
            IProtocolConnection connection,
            CancellationToken cancel
        ) => new(message);

        public ValueTask<IProtocolMessage?> ProcessTx(
            IProtocolMessage message,
            IProtocolConnection connection,
            CancellationToken cancel
        ) =>
            new(
                Replacement != null
                && (ReplaceAtRouter ? connection is IProtocolRouter : connection is IProtocolPort)
                    ? Replacement
                    : message
            );
    }

    private sealed class TestParser : IProtocolParser
    {
        private readonly Subject<IProtocolMessage> _messages = new();
        private readonly Subject<Exception> _errors = new();
        public ProtocolInfo Info { get; } = new("first", "First");
        public IStatistic Statistic { get; } = new Statistic();
        public ProtocolTags Tags { get; } = [];
        public Observable<IProtocolMessage> OnMessage => _messages;
        public Observable<Exception> OnError => _errors;
        public int Disposals { get; private set; }

        public bool Push(byte data) => false;

        public void Reset() { }

        public void Dispose()
        {
            Disposals++;
            _messages.Dispose();
            _errors.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
