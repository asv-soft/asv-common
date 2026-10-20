using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using R3;
using Xunit;

namespace Asv.IO.Test.Connection.Virtual;

// The R3 exception handler is process-wide.
[CollectionDefinition(nameof(VirtualConnectionLifecycleCollection), DisableParallelization = true)]
public sealed class VirtualConnectionLifecycleCollection;

[Collection(nameof(VirtualConnectionLifecycleCollection))]
public class VirtualConnectionLifecycleTest : IDisposable
{
    private readonly Action<Exception> _previousHandler =
        ObservableSystem.GetUnhandledExceptionHandler();
    private readonly ConcurrentQueue<Exception> _unhandled = new();

    public VirtualConnectionLifecycleTest()
    {
        ObservableSystem.RegisterUnhandledExceptionHandler(_unhandled.Enqueue);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Dispose_CompletesBothSidesWithoutUnhandledErrors(
        bool asyncDispose,
        bool printMessages
    )
    {
        await using var link = CreateConnection(printMessages);
        var clientMessages = new List<int>();
        var serverMessages = new List<int>();
        var clientCompleted = 0;
        var serverCompleted = 0;
        using var clientSubscription = link.Client.OnRxMessage.Subscribe(
            message => clientMessages.Add(((ExampleMessage2)message).Value1),
            onCompleted: _ => clientCompleted++
        );
        using var serverSubscription = link.Server.OnRxMessage.Subscribe(
            message => serverMessages.Add(((ExampleMessage2)message).Value1),
            onCompleted: _ => serverCompleted++
        );

        await link.Server.Send(
            new ExampleMessage2 { Value1 = 42 },
            TestContext.Current.CancellationToken
        );
        await link.Client.Send(
            new ExampleMessage2 { Value1 = 73 },
            TestContext.Current.CancellationToken
        );

        await DisposeAsync(link, asyncDispose);
        link.Dispose();
        await link.DisposeAsync();

        Assert.Equal([42], clientMessages);
        Assert.Equal([73], serverMessages);
        Assert.Equal(1, clientCompleted);
        Assert.Equal(1, serverCompleted);
        AssertDisposed(link);
        Assert.Empty(_unhandled);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Dispose_OneSideAlreadyDisposed_DoesNotReportUnhandledErrors(
        bool asyncDispose,
        bool serverFirst
    )
    {
        await using var link = CreateConnection(true);
        var first = serverFirst ? link.Server : link.Client;
        if (asyncDispose)
        {
            await first.DisposeAsync();
        }
        else
        {
            first.Dispose();
        }

        await DisposeAsync(link, asyncDispose);
        link.Dispose();
        await link.DisposeAsync();

        AssertDisposed(link);
        Assert.Empty(_unhandled);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dispose_FromReceiveCallback_DoesNotReportUnhandledErrors(bool serverReceives)
    {
        await using var link = CreateConnection(true);
        var receiver = serverReceives ? link.Server : link.Client;
        var sender = serverReceives ? link.Client : link.Server;
        var received = new List<int>();
        using var subscription = receiver.OnRxMessage.Subscribe(message =>
        {
            received.Add(((ExampleMessage2)message).Value1);
            link.Dispose();
        });

        await sender.Send(
            new ExampleMessage2 { Value1 = 42 },
            TestContext.Current.CancellationToken
        );

        Assert.Equal([42], received);
        AssertDisposed(link);
        Assert.Empty(_unhandled);
    }

    private static IVirtualConnection CreateConnection(bool printMessages)
    {
        return Protocol
            .Create(builder => builder.Protocols.RegisterExampleProtocol())
            .CreateVirtualConnection(printMessagesToLog: printMessages);
    }

    private static async Task DisposeAsync(IVirtualConnection link, bool asyncDispose)
    {
        if (asyncDispose)
        {
            await link.DisposeAsync();
        }
        else
        {
            link.Dispose();
        }
    }

    private static void AssertDisposed(IVirtualConnection link)
    {
        Assert.True(Assert.IsType<VirtualPort>(link.Server).IsDisposed);
        Assert.True(Assert.IsType<VirtualPort>(link.Client).IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => link.Server.OnRxMessage.Subscribe(_ => { }));
        Assert.Throws<ObjectDisposedException>(() => link.Client.OnRxMessage.Subscribe(_ => { }));
    }

    public void Dispose()
    {
        ObservableSystem.RegisterUnhandledExceptionHandler(_previousHandler);
    }
}
