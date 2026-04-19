using JetBrains.Annotations;
using ObservableCollections;

namespace Asv.Modeling.Test;

[TestSubject(typeof(NavigationController<IViewModel>))]
public class NavigationControllerTest
{
    [Fact]
    public async ValueTask GoTo_NavigatesUsingCurrentNodeForNestedPath()
    {
        var (root, child1, child2) = CreateTree();
        var store = new InMemoryNavigationStore();
        using var controller = new NavigationController<IViewModel>(root, store);

        var result = await controller.GoTo(new NavPath(root.Id, child1.Id, child2.Id));

        Assert.Same(child2, result);
        Assert.Same(child2, controller.SelectedControl.CurrentValue);
        Assert.Equal(new NavPath(root.Id, child1.Id, child2.Id), controller.SelectedPath.CurrentValue);
    }

    [Fact]
    public async ValueTask NavigateEvent_AddsPreviousPathToBackwardAndClearsForward()
    {
        var (root, child1, child2) = CreateTree();
        var store = new InMemoryNavigationStore();
        using var controller = new NavigationController<IViewModel>(root, store);

        await root.Rise(
            new NavigateEvent<IViewModel>(root, new NavPath(root.Id, child1.Id)),
            TestContext.Current.CancellationToken
        );
        await root.Rise(
            new NavigateEvent<IViewModel>(root, new NavPath(root.Id, child1.Id, child2.Id)),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(
            [
                new NavPath(root.Id),
                new NavPath(root.Id, child1.Id),
            ],
            controller.BackwardStack.Cast<NavPath>().Reverse().ToArray()
        );
        Assert.Empty(controller.ForwardStack);
        Assert.Equal(new NavPath(root.Id, child1.Id, child2.Id), controller.SelectedPath.CurrentValue);
    }

    [Fact]
    public async ValueTask BackwardForward_UseCurrentSelectionAsOppositeHistoryEntry()
    {
        var (root, child1, child2) = CreateTree();
        var store = new InMemoryNavigationStore();
        using var controller = new NavigationController<IViewModel>(root, store);

        await root.Rise(
            new NavigateEvent<IViewModel>(root, new NavPath(root.Id, child1.Id)),
            TestContext.Current.CancellationToken
        );
        await root.Rise(
            new NavigateEvent<IViewModel>(root, new NavPath(root.Id, child1.Id, child2.Id)),
            TestContext.Current.CancellationToken
        );

        await controller.BackwardAsync();

        Assert.Equal(new NavPath(root.Id, child1.Id), controller.SelectedPath.CurrentValue);
        Assert.Equal([new NavPath(root.Id, child1.Id, child2.Id)], controller.ForwardStack.Cast<NavPath>().Reverse().ToArray());
        Assert.Equal([new NavPath(root.Id)], controller.BackwardStack.Cast<NavPath>().Reverse().ToArray());

        await controller.ForwardAsync();

        Assert.Equal(new NavPath(root.Id, child1.Id, child2.Id), controller.SelectedPath.CurrentValue);
        Assert.Equal([new NavPath(root.Id), new NavPath(root.Id, child1.Id)], controller.BackwardStack.Cast<NavPath>().Reverse().ToArray());
        Assert.Empty(controller.ForwardStack);
    }

    private static (TestNavigationViewModel Root, TestNavigationViewModel Child1, TestNavigationViewModel Child2) CreateTree()
    {
        var root = new TestNavigationViewModel("root");
        var child1 = new TestNavigationViewModel("child1");
        var child2 = new TestNavigationViewModel("child2");
        root.AddChild(child1);
        child1.AddChild(child2);
        return (root, child1, child2);
    }

    private sealed class InMemoryNavigationStore : INavigationStore
    {
        private readonly List<NavPath> _forward = [];
        private readonly List<NavPath> _backward = [];

        public void Load(Action<NavPath> addForward, Action<NavPath> addBackward)
        {
            foreach (var path in _forward)
            {
                addForward(path);
            }

            foreach (var path in _backward)
            {
                addBackward(path);
            }
        }

        public void Save(IEnumerable<NavPath> forward, IEnumerable<NavPath> backward)
        {
            _forward.Clear();
            _forward.AddRange(forward);
            _backward.Clear();
            _backward.AddRange(backward);
        }
    }

    private sealed class TestNavigationViewModel : ViewModelBase
    {
        public TestNavigationViewModel(string id)
            : base(id)
        {
        }

        public ObservableList<IViewModel> Children { get; } = new();

        public void AddChild(TestNavigationViewModel child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public override IEnumerable<IViewModel> GetChildren()
        {
            return Children;
        }
    }
}
