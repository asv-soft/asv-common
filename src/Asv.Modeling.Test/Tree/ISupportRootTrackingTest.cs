using JetBrains.Annotations;
using R3;

namespace Asv.Modeling.Test;

[TestSubject(typeof(ISupportRootTracking<,>))]
public class ISupportRootTrackingTest
{
    [Fact]
    public void RootTracking_RootNodeTracksItself()
    {
        using var root = new TestRoot();

        Assert.Same(root, root.RootTracking.Root.CurrentValue);
    }

    [Fact]
    public void RootTracking_ChildTracksRoot_WhenParentIsRoot()
    {
        using var root = new TestRoot();
        using var child = new TestNode();

        root.AddChild(child);

        Assert.Same(root, child.RootTracking.Root.CurrentValue);
    }

    [Fact]
    public void RootTracking_ChildTracksRoot_WhenParentIsAlreadyAttachedNode()
    {
        using var root = new TestRoot();
        using var parent = new TestNode();
        using var child = new TestNode();
        root.AddChild(parent);

        parent.AddChild(child);

        Assert.Same(root, parent.RootTracking.Root.CurrentValue);
        Assert.Same(root, child.RootTracking.Root.CurrentValue);
    }

    [Fact]
    public void RootTracking_ExistingSubtreeTracksRoot_WhenParentIsAttachedLater()
    {
        using var root = new TestRoot();
        using var parent = new TestNode();
        using var child = new TestNode();
        parent.AddChild(child);

        root.AddChild(parent);

        Assert.Same(root, parent.RootTracking.Root.CurrentValue);
        Assert.Same(root, child.RootTracking.Root.CurrentValue);
    }

    [Fact]
    public void RootTracking_DetachClearsRootForSubtree()
    {
        using var root = new TestRoot();
        using var parent = new TestNode();
        using var child = new TestNode();
        root.AddChild(parent);
        parent.AddChild(child);

        root.RemoveChild(parent);

        Assert.Null(parent.RootTracking.Root.CurrentValue);
        Assert.Null(child.RootTracking.Root.CurrentValue);
    }

    [Fact]
    public void ExecuteWhenRootAttached_ExecutesImmediately_WhenRootExists()
    {
        using var root = new TestRoot();
        using var child = new TestNode();
        root.AddChild(child);
        TestRoot? actual = null;

        using var subscription = child.RootTracking.ExecuteWhenRootAttached(root => actual = root);

        Assert.Same(root, actual);
    }

    [Fact]
    public void ExecuteWhenRootAttached_ExecutesWhenRootAppears()
    {
        using var root = new TestRoot();
        using var child = new TestNode();
        TestRoot? actual = null;

        using var subscription = child.RootTracking.ExecuteWhenRootAttached(root => actual = root);
        root.AddChild(child);

        Assert.Same(root, actual);
    }

    [Fact]
    public void ExecuteWhenRootAttached_ExecutesEveryTimeRootAppears()
    {
        using var root = new TestRoot();
        using var child = new TestNode();
        var count = 0;

        using var subscription = child.RootTracking.ExecuteWhenRootAttached(_ => count++);
        root.AddChild(child);
        root.RemoveChild(child);
        root.AddChild(child);

        Assert.Equal(2, count);
    }

    [Fact]
    public void ExecuteWhenRootAttached_ExecutesWhenRootChanges()
    {
        using var root1 = new TestRoot();
        using var root2 = new TestRoot();
        using var child = new TestNode();
        var roots = new List<TestRoot>();

        using var subscription = child.RootTracking.ExecuteWhenRootAttached(roots.Add);
        root1.AddChild(child);
        root1.RemoveChild(child);
        root2.AddChild(child);

        Assert.Equal([root1, root2], roots);
    }

    [Fact]
    public void ExecuteWhenRootAttached_DoesNotExecute_WhenSubscriptionDisposedBeforeAttach()
    {
        using var root = new TestRoot();
        using var child = new TestNode();
        var count = 0;

        var subscription = child.RootTracking.ExecuteWhenRootAttached(_ => count++);
        subscription.Dispose();
        root.AddChild(child);

        Assert.Equal(0, count);
    }

    [Fact]
    public void ExecuteWhenRootAttachedAsync_ExecutesImmediatelyOnce_WhenRootExists()
    {
        using var root = new TestRoot();
        using var child = new TestNode();
        root.AddChild(child);
        TestRoot? actual = null;
        var count = 0;

        using var subscription = child.RootTracking.ExecuteWhenRootAttached(root =>
        {
            actual = root;
            count++;
            return ValueTask.CompletedTask;
        });

        Assert.Same(root, actual);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ExecuteWhenRootAttachedAsync_ExecutesWhenRootAppears()
    {
        using var root = new TestRoot();
        using var child = new TestNode();
        var actual = new TaskCompletionSource<TestRoot>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        using var subscription = child.RootTracking.ExecuteWhenRootAttached(async root =>
        {
            await Task.Yield();
            actual.SetResult(root);
        });
        root.AddChild(child);

        Assert.Same(
            root,
            await actual.Task.WaitAsync(
                TimeSpan.FromSeconds(1),
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public void ExecuteWhenRootAttachedAsync_ExecutesEveryTimeRootAppears()
    {
        using var root = new TestRoot();
        using var child = new TestNode();
        var count = 0;

        using var subscription = child.RootTracking.ExecuteWhenRootAttached(_ =>
        {
            count++;
            return ValueTask.CompletedTask;
        });
        root.AddChild(child);
        root.RemoveChild(child);
        root.AddChild(child);

        Assert.Equal(2, count);
    }

    [Fact]
    public void ExecuteWhenRootAttachedAsync_DoesNotExecute_WhenSubscriptionDisposedBeforeAttach()
    {
        using var root = new TestRoot();
        using var child = new TestNode();
        var count = 0;

        var subscription = child.RootTracking.ExecuteWhenRootAttached(_ =>
        {
            count++;
            return ValueTask.CompletedTask;
        });
        subscription.Dispose();
        root.AddChild(child);

        Assert.Equal(0, count);
    }

    [Fact]
    public void ExecuteWhenRootAttachedAsync_WithCancellationToken_ExecutesWhenRootAppears()
    {
        using var root = new TestRoot();
        using var child = new TestNode();
        TestRoot? actual = null;

        using var subscription = child.RootTracking.ExecuteWhenRootAttached(
            (root, _) =>
            {
                actual = root;
                return ValueTask.CompletedTask;
            }
        );
        root.AddChild(child);

        Assert.Same(root, actual);
    }

    [Fact]
    public void Attached_EmitsRoot_WhenRootAppears()
    {
        using var root = new TestRoot();
        using var child = new TestNode();
        var attachedRoots = new List<TestRoot>();
        using var subscription = child.RootTracking.Attached.Subscribe(attachedRoots.Add);

        root.AddChild(child);

        Assert.Equal([root], attachedRoots);
    }

    [Fact]
    public void Attached_DoesNotEmit_WhenRootIsDetached()
    {
        using var root = new TestRoot();
        using var child = new TestNode();
        var attachedRoots = new List<TestRoot>();
        using var subscription = child.RootTracking.Attached.Subscribe(attachedRoots.Add);

        root.AddChild(child);
        root.RemoveChild(child);

        Assert.Equal([root], attachedRoots);
    }

    [Fact]
    public void Detached_Emits_WhenRootIsCleared()
    {
        using var root = new TestRoot();
        using var child = new TestNode();
        root.AddChild(child);
        var detachCount = 0;
        using var subscription = child.RootTracking.Detached.Subscribe(_ => detachCount++);

        root.RemoveChild(child);

        Assert.Equal(1, detachCount);
    }

    private class TestNode
        : ISupportParentChange<TestNode>,
            ISupportRoutedEvents<TestNode>,
            ISupportRootTracking<TestNode, TestRoot>,
            IDisposable
    {
        private readonly List<TestNode> _children = [];
        private readonly Subject<TestNode?> _parentChanged = new();
        private readonly RoutedEventController<TestNode> _events;
        private readonly RootTrackingController<TestNode, TestRoot> _rootTracking;

        public TestNode()
        {
            _events = new RoutedEventController<TestNode>(this);
            _rootTracking = new RootTrackingController<TestNode, TestRoot>(this);
        }

        public IRoutedEventController<TestNode> Events => _events;

        public TestNode? Parent { get; private set; }

        public Observable<TestNode?> ParentChanged => _parentChanged;

        public IRootTrackingController<TestRoot> RootTracking => _rootTracking;

        public IEnumerable<TestNode> GetChildren()
        {
            return _children;
        }

        public void AddChild(TestNode child)
        {
            ArgumentNullException.ThrowIfNull(child);
            _children.Add(child);
            child.SetParent(this);
        }

        public void RemoveChild(TestNode child)
        {
            ArgumentNullException.ThrowIfNull(child);
            if (_children.Remove(child))
            {
                child.SetParent(null);
            }
        }

        public void SetParent(TestNode? parent)
        {
            if (ReferenceEquals(Parent, parent))
            {
                return;
            }

            Parent = parent;
            _parentChanged.OnNext(parent);
        }

        public void Dispose()
        {
            _rootTracking.Dispose();
            _events.Dispose();
            _parentChanged.Dispose();
        }
    }

    private sealed class TestRoot : TestNode { }
}
