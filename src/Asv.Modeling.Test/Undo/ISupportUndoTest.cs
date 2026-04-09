using Asv.Common;
using JetBrains.Annotations;
using ObservableCollections;
using R3;

namespace Asv.Modeling.Test;

[TestSubject(typeof(ISupportUndo<,>))]
public class ISupportUndoTest
{
    [Fact]
    public async ValueTask UndoRedoTests()
    {
        var storageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var root = new RootViewModel("root", storageDirectory);
        var child1 = new ViewModelBase("child1");
        root.Children.Add(child1);
        var child2 = new ViewModelBase("child2");
        child1.Children.Add(child2);
        var child3 = new ViewModelBase("child3");
        child2.Children.Add(child3);

        var longString = NavigationId.GenerateRandomAsString(5 * 1024);

        child3.Prop1.Value = "1";
        child3.Prop1.Value = "2";
        child3.Prop1.Value = longString;

        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);
        Assert.Equal("2", child3.Prop1.Value);
        await root.UndoHistory.RedoAsync(TestContext.Current.CancellationToken);
        Assert.Equal(longString, child3.Prop1.Value);

        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);
        Assert.Equal("2", child3.Prop1.Value);
        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);
        Assert.Equal("1", child3.Prop1.Value);
        await root.DisposeAsync();
    }

    [Fact]
    public async ValueTask UndoRedoRestoreTests()
    {
        var storageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var root = new RootViewModel("root", storageDirectory);
        var child1 = new ViewModelBase("child1");
        root.Children.Add(child1);
        var child2 = new ViewModelBase("child2");
        child1.Children.Add(child2);
        var child3 = new ViewModelBase("child3");
        child2.Children.Add(child3);

        var longString = NavigationId.GenerateRandomAsString(5 * 1024);

        child3.Prop1.Value = "1";
        child3.Prop1.Value = "2";
        child3.Prop1.Value = longString;

        await root.DisposeAsync();

        // now it's restore stack
        root = new RootViewModel("root", storageDirectory);
        child1 = new ViewModelBase("child1");
        root.Children.Add(child1);
        child2 = new ViewModelBase("child2");
        child1.Children.Add(child2);
        child3 = new ViewModelBase("child3");
        child2.Children.Add(child3);

        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);
        Assert.Equal("2", child3.Prop1.Value);
        await root.UndoHistory.RedoAsync(TestContext.Current.CancellationToken);
        Assert.Equal(longString, child3.Prop1.Value);

        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);
        Assert.Equal("2", child3.Prop1.Value);
        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);
        Assert.Equal("1", child3.Prop1.Value);
        await root.DisposeAsync();
    }

    [Fact]
    public async ValueTask UndoRedo_ShouldNotCreateNewHistoryRecords_WhenHandlerMutesChanges()
    {
        var storageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var root = new RootViewModel("root", storageDirectory);
        var child1 = new ViewModelBase("child1");
        root.Children.Add(child1);
        var child2 = new ViewModelBase("child2");
        child1.Children.Add(child2);
        var child3 = new ViewModelBase("child3");
        child2.Children.Add(child3);

        child3.Prop1.Value = "1";
        child3.Prop1.Value = "2";
        child3.Prop1.Value = "3";

        Assert.Equal(3, root.UndoHistory.UndoStack.Count);
        Assert.Empty(root.UndoHistory.RedoStack);

        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);

        Assert.Equal("2", child3.Prop1.Value);
        Assert.Equal(2, root.UndoHistory.UndoStack.Count);
        Assert.Single(root.UndoHistory.RedoStack);

        await root.UndoHistory.RedoAsync(TestContext.Current.CancellationToken);

        Assert.Equal("3", child3.Prop1.Value);
        Assert.Equal(3, root.UndoHistory.UndoStack.Count);
        Assert.Empty(root.UndoHistory.RedoStack);

        await root.DisposeAsync();
    }

    [Fact]
    public async ValueTask UndoRedoRestore_ShouldNotCreateNewHistoryRecords_WhenHandlerMutesChanges()
    {
        var storageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var root = new RootViewModel("root", storageDirectory);
        var child1 = new ViewModelBase("child1");
        root.Children.Add(child1);
        var child2 = new ViewModelBase("child2");
        child1.Children.Add(child2);
        var child3 = new ViewModelBase("child3");
        child2.Children.Add(child3);

        child3.Prop1.Value = "1";
        child3.Prop1.Value = "2";
        child3.Prop1.Value = "3";

        await root.DisposeAsync();

        root = new RootViewModel("root", storageDirectory);
        child1 = new ViewModelBase("child1");
        root.Children.Add(child1);
        child2 = new ViewModelBase("child2");
        child1.Children.Add(child2);
        child3 = new ViewModelBase("child3");
        child2.Children.Add(child3);

        Assert.Equal(3, root.UndoHistory.UndoStack.Count);
        Assert.Empty(root.UndoHistory.RedoStack);

        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);

        Assert.Equal("2", child3.Prop1.Value);
        Assert.Equal(2, root.UndoHistory.UndoStack.Count);
        Assert.Single(root.UndoHistory.RedoStack);

        await root.UndoHistory.RedoAsync(TestContext.Current.CancellationToken);

        Assert.Equal("3", child3.Prop1.Value);
        Assert.Equal(3, root.UndoHistory.UndoStack.Count);
        Assert.Empty(root.UndoHistory.RedoStack);

        await root.DisposeAsync();
    }
}

public class RootViewModel : ViewModelBase, IHasUndoHistory<ViewModelBase, string>
{
    public RootViewModel(string id, string storageDirectory)
        : base(id)
    {
        UndoHistory = new UndoHistory<ViewModelBase, string>(
            this,
            new JsonUndoHistoryStore<string>(storageDirectory, static id => id, static id => id)
        ).AddTo(ref DisposableBag);
    }

    public IUndoHistory<ViewModelBase, string> UndoHistory { get; }
}

public class ViewModelBase : AsyncDisposableOnceBag, ISupportUndo<ViewModelBase, string>
{
    public ViewModelBase(string id)
    {
        Id = id;
        Events = new RoutedEventController<ViewModelBase>(this);
        Undo = new UndoController<ViewModelBase>(this).AddTo(ref DisposableBag);
        Children.SetParent(this).AddTo(ref DisposableBag);
        Children.DisposeRemovedItems().AddTo(ref DisposableBag);

        Undo.Register(nameof(Prop1), Prop1).AddTo(ref DisposableBag);
        Undo.EnableChangePublication();
    }

    public string Id { get; }
    public IUndoController Undo { get; }
    public IRoutedEventController<ViewModelBase> Events { get; }
    public ViewModelBase Parent { get; set; }
    public ObservableList<ViewModelBase> Children { get; } = new();

    public IEnumerable<ViewModelBase> GetChildren()
    {
        return Children;
    }

    public ValueTask<ViewModelBase> Navigate(string id)
    {
        return ValueTask.FromResult(GetChildren().FirstOrDefault(c => c.Id == id));
    }

    public ReactiveProperty<string> Prop1 { get; } = new();

    public override string ToString()
    {
        return Id;
    }
}
