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

        child3.Undo.EnablePublication();
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
        Children.SetParent<IViewModel,IViewModel>(this).AddTo(ref DisposableBag);
        Children.DisposeRemovedItems().AddTo(ref DisposableBag);

        Undo.CreateAndRegister(nameof(Prop1), Prop1).AddTo(ref DisposableBag);
        Undo.EnablePublication();
    }

    public ObservableList<IViewModel> Children { get; } = new();

    public override IEnumerable<IViewModel> GetChildren()
    {
        return Children;
    }
    public ReactiveProperty<string> Prop1 { get; } = new();


   
}
