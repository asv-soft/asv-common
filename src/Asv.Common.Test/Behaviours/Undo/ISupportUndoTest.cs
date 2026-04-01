using System.Collections.Generic;
using Asv.Common;
using JetBrains.Annotations;
using R3;
using Xunit;

namespace Asv.Common.Test.Behaviours.Undo;

[TestSubject(typeof(ISupportUndo<,>))]
public class ISupportUndoTest
{

    [Fact]
    public void METHOD()
    {
        
    }
}

public class TestEventBase : AsyncRoutedEvent<TestViewModelBase>
{
    public TestEventBase(TestViewModelBase sender, RoutingStrategy strategy)
        : base(sender, strategy)
    {
    }
}

public class TestViewModelBase : ISupportUndo<TestViewModelBase, TestEventBase>, ISupportRoutedEvents<TestViewModelBase>
{
    public TestViewModelBase()
    {
        Events = new RoutedEventController<TestViewModelBase>(this);
        Undo = new UndoController<TestViewModelBase, TestEventBase>(this);
    }
    public IUndoController<TestViewModelBase, TestEventBase> Undo { get; }
    public TestViewModelBase Parent { get; set; }
    public IEnumerable<TestViewModelBase> GetChildren()
    {
        yield break;
    }
    public IRoutedEventController<TestViewModelBase> Events { get; }
}

public class ViewModelWithUndoProperty : TestViewModelBase
{
    public ViewModelWithUndoProperty()
    {
        Undo.Register(nameof(UndoProperty), UndoProperty);
    }
    
    public ReactiveProperty<string> UndoProperty { get; } = new();
}
