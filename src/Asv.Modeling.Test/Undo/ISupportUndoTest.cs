using Asv.Common;
using JetBrains.Annotations;
using ObservableCollections;
using R3;

namespace Asv.Modeling.Test;

[TestSubject(typeof(ISupportUndo<>))]
public class ISupportUndoTest
{
    [Fact]
    public async ValueTask UndoRedoTests()
    {
        var storageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var root = new HistoryViewModel("root", storageDirectory);
        var child1 = new TestViewModelBase("child1");
        root.Children.Add(child1);
        var child2 = new TestViewModelBase("child2");
        child1.Children.Add(child2);
        var child3 = new TestViewModelBase("child3");
        child2.Children.Add(child3);

        var longString = NavId.GenerateRandomAsString(5 * 1024);

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
        root.Dispose();
    }

    [Fact]
    public async ValueTask UndoRedoRestoreTests()
    {
        var storageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var root = new HistoryViewModel("root", storageDirectory);
        var child1 = new TestViewModelBase("child1");
        root.Children.Add(child1);
        var child2 = new TestViewModelBase("child2");
        child1.Children.Add(child2);
        var child3 = new TestViewModelBase("child3");
        child2.Children.Add(child3);

        var longString = NavId.GenerateRandomAsString(5 * 1024);

        child3.Prop1.Value = "1";
        child3.Prop1.Value = "2";
        child3.Prop1.Value = longString;

        root.Dispose();

        // now it's restore stack
        root = new HistoryViewModel("root", storageDirectory);
        child1 = new TestViewModelBase("child1");
        root.Children.Add(child1);
        child2 = new TestViewModelBase("child2");
        child1.Children.Add(child2);
        child3 = new TestViewModelBase("child3");
        child2.Children.Add(child3);

        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);
        Assert.Equal("2", child3.Prop1.Value);
        await root.UndoHistory.RedoAsync(TestContext.Current.CancellationToken);
        Assert.Equal(longString, child3.Prop1.Value);

        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);
        Assert.Equal("2", child3.Prop1.Value);
        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);
        Assert.Equal("1", child3.Prop1.Value);
        root.Dispose();
    }

    [Fact]
    public async ValueTask UndoRedo_ShouldNotCreateNewHistoryRecords_WhenHandlerMutesChanges()
    {
        var storageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var root = new HistoryViewModel("root", storageDirectory);
        var child1 = new TestViewModelBase("child1");
        root.Children.Add(child1);
        var child2 = new TestViewModelBase("child2");
        child1.Children.Add(child2);
        var child3 = new TestViewModelBase("child3");
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

        root.Dispose();
    }

    [Fact]
    public async ValueTask UndoRedoRestore_ShouldNotCreateNewHistoryRecords_WhenHandlerMutesChanges()
    {
        var storageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var root = new HistoryViewModel("root", storageDirectory);
        var child1 = new TestViewModelBase("child1");
        root.Children.Add(child1);
        var child2 = new TestViewModelBase("child2");
        child1.Children.Add(child2);
        var child3 = new TestViewModelBase("child3");
        child2.Children.Add(child3);

        child3.Prop1.Value = "1";
        child3.Prop1.Value = "2";
        child3.Prop1.Value = "3";

        root.Dispose();

        root = new HistoryViewModel("root", storageDirectory);
        child1 = new TestViewModelBase("child1");
        root.Children.Add(child1);
        child2 = new TestViewModelBase("child2");
        child1.Children.Add(child2);
        child3 = new TestViewModelBase("child3");
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

        root.Dispose();
    }

    [Fact]
    public void SuppressChangePublication_DoesNotCreateHistoryRecordsInsideScope()
    {
        var storageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var root = new HistoryViewModel("root", storageDirectory);
        var child = new TestViewModelBase("child");
        root.Children.Add(child);

        child.Prop1.Value = "1";

        using (child.Undo.SuppressChangePublication())
        {
            child.Prop1.Value = "2";
            using (child.Undo.SuppressChangePublication())
            {
                child.Prop1.Value = "3";
            }
            child.Prop1.Value = "4";
        }

        child.Prop1.Value = "5";

        Assert.Equal(2, root.UndoHistory.UndoStack.Count);
        Assert.Empty(root.UndoHistory.RedoStack);

        root.Dispose();
    }

    [Fact]
    public async ValueTask ObservableListAdd_UndoRedo_RestoresCollection()
    {
        var storageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var root = new HistoryViewModel("root", storageDirectory);
        var child = new TestViewModelBase("child");
        root.Children.Add(child);

        child.Prop2.Add("first");
        child.Prop2.Add("second");

        Assert.Equal(["first", "second"], child.Prop2);
        Assert.Equal(2, root.UndoHistory.UndoStack.Count);

        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["first"], child.Prop2);
        Assert.Single(root.UndoHistory.UndoStack);
        Assert.Single(root.UndoHistory.RedoStack);

        await root.UndoHistory.RedoAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["first", "second"], child.Prop2);
        Assert.Equal(2, root.UndoHistory.UndoStack.Count);
        Assert.Empty(root.UndoHistory.RedoStack);

        root.Dispose();
    }

    [Fact]
    public async ValueTask ObservableListRemove_UndoRedo_RestoresCollection()
    {
        var storageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var root = new HistoryViewModel("root", storageDirectory);
        var child = new TestViewModelBase("child");
        root.Children.Add(child);

        child.Prop2.Add("first");
        child.Prop2.Add("second");
        child.Prop2.RemoveAt(0);

        Assert.Equal(["second"], child.Prop2);
        Assert.Equal(3, root.UndoHistory.UndoStack.Count);

        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["first", "second"], child.Prop2);
        Assert.Equal(2, root.UndoHistory.UndoStack.Count);
        Assert.Single(root.UndoHistory.RedoStack);

        await root.UndoHistory.RedoAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["second"], child.Prop2);
        Assert.Equal(3, root.UndoHistory.UndoStack.Count);
        Assert.Empty(root.UndoHistory.RedoStack);

        root.Dispose();
    }

    [Fact]
    public async ValueTask ObservableListInsertRange_UndoRedo_RestoresCollection()
    {
        var storageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var root = new HistoryViewModel("root", storageDirectory);
        var child = new TestViewModelBase("child");
        root.Children.Add(child);

        child.Prop2.Add("first");
        child.Prop2.InsertRange(1, ["second", "third"]);

        Assert.Equal(["first", "second", "third"], child.Prop2);
        Assert.Equal(2, root.UndoHistory.UndoStack.Count);

        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["first"], child.Prop2);
        Assert.Single(root.UndoHistory.UndoStack);
        Assert.Single(root.UndoHistory.RedoStack);

        await root.UndoHistory.RedoAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["first", "second", "third"], child.Prop2);
        Assert.Equal(2, root.UndoHistory.UndoStack.Count);
        Assert.Empty(root.UndoHistory.RedoStack);

        root.Dispose();
    }
}

public class HistoryViewModel : UndoHistoryViewModel
{
    public HistoryViewModel(string id, string storageDirectory)
        : base(id, storageDirectory)
    {
        Children.SetParent<IViewModel, IViewModel>(this).AddTo(ref DisposableBag);
        Children.DisposeRemovedItems().AddTo(ref DisposableBag);
    }

    public ObservableList<IViewModel> Children { get; } = new();

    public override IEnumerable<IViewModel> GetChildren()
    {
        return Children;
    }
}

public class TestViewModelBase : UndoableViewModel
{
    public TestViewModelBase(string id)
        : base(id)
    {
        Children.SetParent<IViewModel, IViewModel>(this).AddTo(ref DisposableBag);
        Children.DisposeRemovedItems().AddTo(ref DisposableBag);

        Undo.Create(nameof(Prop1), Prop1).AddTo(ref DisposableBag);
        Undo.Create(nameof(Prop2), Prop2).AddTo(ref DisposableBag);
    }

    public ObservableList<IViewModel> Children { get; } = new();

    public override IEnumerable<IViewModel> GetChildren()
    {
        return Children;
    }

    public ReactiveProperty<string> Prop1 { get; } = new();
    public ObservableList<string> Prop2 { get; } = new();
}
