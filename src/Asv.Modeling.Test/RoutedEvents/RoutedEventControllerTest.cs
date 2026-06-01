namespace Asv.Modeling.Test;

public class RoutedEventControllerTest
{
    [Fact]
    public async Task Rise_Throws_WhenEventIsNull()
    {
        var node = new TestNode();

        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await node.Events.Rise(null!, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public void Catch_Throws_WhenHandlerIsNull()
    {
        var node = new TestNode();

        Assert.Throws<ArgumentNullException>(() => node.Events.Catch(null!));
        Assert.Throws<ArgumentNullException>(() =>
            node.Events.Catch<TestEvent>((RoutedEventHandler<TestNode, TestEvent>)null!)
        );
        Assert.Throws<ArgumentNullException>(() =>
            node.Events.Catch<TestEvent>((Action<TestEvent>)null!)
        );
    }

    [Fact]
    public async Task Rise_ThrowsCanceled_BeforeInvokingHandlers()
    {
        var node = new TestNode();
        var called = false;
        using var cancel = new CancellationTokenSource();
        using var subscription = node.Events.Catch(
            (_, _, _) =>
            {
                called = true;
                return ValueTask.CompletedTask;
            }
        );

        await cancel.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await node.Events.Rise(new TestEvent(node, RoutingStrategy.Direct), cancel.Token)
        );
        Assert.False(called);
    }

    [Fact]
    public async Task Rise_AwaitsEachMulticastHandlerBeforeContinuing()
    {
        var node = new TestNode();
        var firstStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var releaseFirst = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var calls = new List<string>();
        using var first = node.Events.Catch(
            async (_, _, _) =>
            {
                calls.Add("first-start");
                firstStarted.SetResult();
                await releaseFirst.Task;
                calls.Add("first-end");
            }
        );
        using var second = node.Events.Catch(
            (_, _, _) =>
            {
                calls.Add("second");
                return ValueTask.CompletedTask;
            }
        );

        var rise = node
            .Events.Rise(
                new TestEvent(node, RoutingStrategy.Direct),
                TestContext.Current.CancellationToken
            )
            .AsTask();

        await firstStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken
        );
        Assert.False(rise.IsCompleted);
        Assert.Equal(["first-start"], calls);

        releaseFirst.SetResult();
        await rise.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(["first-start", "first-end", "second"], calls);
    }

    [Fact]
    public async Task Rise_PropagatesHandlerException_AndDoesNotInvokeNextHandler()
    {
        var node = new TestNode();
        var nextCalled = false;
        using var first = node.Events.Catch(
            async (_, _, _) =>
            {
                await Task.Yield();
                throw new InvalidOperationException("handler failed");
            }
        );
        using var second = node.Events.Catch(
            (_, _, _) =>
            {
                nextCalled = true;
                return ValueTask.CompletedTask;
            }
        );

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await node.Events.Rise(
                new TestEvent(node, RoutingStrategy.Direct),
                TestContext.Current.CancellationToken
            )
        );

        Assert.Equal("handler failed", exception.Message);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Rise_StopsHandlersAndBubbleRouting_WhenEventIsHandled()
    {
        var parent = new TestNode();
        var child = new TestNode();
        parent.AddChild(child);
        var secondChildHandlerCalled = false;
        var parentHandlerCalled = false;
        using var first = child.Events.Catch<TestEvent>(e => e.IsHandled = true);
        using var second = child.Events.Catch<TestEvent>(_ => secondChildHandlerCalled = true);
        using var parentSubscription = parent.Events.Catch<TestEvent>(_ =>
            parentHandlerCalled = true
        );

        await child.Events.Rise(
            new TestEvent(child, RoutingStrategy.Bubble),
            TestContext.Current.CancellationToken
        );

        Assert.False(secondChildHandlerCalled);
        Assert.False(parentHandlerCalled);
    }

    private sealed class TestEvent(TestNode sender, RoutingStrategy strategy)
        : AsyncRoutedEvent<TestNode>(sender, strategy);

    private sealed class TestNode : ISupportRoutedEvents<TestNode>
    {
        private readonly List<TestNode> _children = [];

        public TestNode()
        {
            Events = new RoutedEventController<TestNode>(this);
        }

        public TestNode? Parent { get; private set; }
        public IRoutedEventController<TestNode> Events { get; }

        public IEnumerable<TestNode> GetChildren()
        {
            return _children;
        }

        public void AddChild(TestNode child)
        {
            child.Parent = this;
            _children.Add(child);
        }
    }
}
