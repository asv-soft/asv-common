using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asv.Common;
using Asv.Store.Undo.History.Store;
using JetBrains.Annotations;
using ObservableCollections;
using R3;
using Xunit;

namespace Asv.Common.Test.Behaviours.Undo;

[TestSubject(typeof(ISupportUndo<,>))]
public class ISupportUndoTest
{
    [Fact]
    public async ValueTask UndoRedoTests()
    {
        var root = new RootViewModel("root");
        var child1 = new ViewModelBase("child1");
        root.Children.Add(child1);
        var child2 = new ViewModelBase("child2");
        child1.Children.Add(child2);
        var child3 = new ViewModelBase("child3");
        child2.Children.Add(child3);

        child3.Prop1.Value = "1";
        child3.Prop1.Value = "2";
        child3.Prop1.Value = "3";

        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);
        Assert.Equal("2", child3.Prop1.Value);
        await root.UndoHistory.RedoAsync(TestContext.Current.CancellationToken);
        Assert.Equal("3", child3.Prop1.Value);

        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);
        Assert.Equal("2", child3.Prop1.Value);
        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);
        Assert.Equal("1", child3.Prop1.Value);
    }
}

public class RootViewModel : ViewModelBase, IHasUndoHistory<ViewModelBase, string>
{
    public RootViewModel(string id)
        : base(id)
    {
        UndoHistory = new UndoHistory<ViewModelBase, string>(
            this,
            new MemoryPackUndoHistoryStore<string>()
        );
    }

    public IUndoHistory<ViewModelBase, string> UndoHistory { get; }
}

public class ViewModelBase : AsyncDisposableOnceBag, ISupportUndo<ViewModelBase, string>
{
    public ViewModelBase(string id)
    {
        Id = id;
        Events = new RoutedEventController<ViewModelBase>(this);
        Undo = new UndoController<ViewModelBase>(this);
        Children.SetParent(this).AddTo(ref DisposableBag);

        Undo.Register(nameof(Prop1), Prop1).AddTo(ref DisposableBag);
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
