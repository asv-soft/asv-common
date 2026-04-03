using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asv.Common;
using JetBrains.Annotations;
using ObservableCollections;
using R3;
using Xunit;

namespace Asv.Common.Test.Behaviours.Undo;

[TestSubject(typeof(ISupportUndo<,>))]
public class ISupportUndoTest
{
    [Fact]
    public void METHOD()
    {
        var root = new RootViewModel("root");
        var child = new ViewModelBase("child");
        root.Children.Add(child);

        child.Prop1.Value = "123";
    }
}

public class RootViewModel : ViewModelBase, ISupportUndoHistory<ViewModelBase, string>
{
    public RootViewModel(string id)
        : base(id)
    {
        UndoHistory = new UndoHistory<ViewModelBase, string>(this, new UndoHistoryStore<string>());
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
}
