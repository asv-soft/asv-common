using System.Collections.Generic;
using Asv.Common;
using JetBrains.Annotations;
using Xunit;

namespace Asv.Common.Test.UndoPattern;

public class TestEventBase { }

public interface IViewModel : ISupportRoutedEvents<IViewModel> { }

public class ViewModel : IViewModel
{
    public ViewModel()
    {
        Events = new RoutedEventController<IViewModel>(this);
    }

    public IViewModel Parent { get; set; }

    public IEnumerable<IViewModel> GetChildren()
    {
        throw new System.NotImplementedException();
    }

    public IRoutedEventController<IViewModel> Events { get; }
}

[TestSubject(typeof(UndoManager))]
public class UndoManagerTest
{
    [Fact]
    public void METHOD()
    {
        var vm = new ViewModel();

        var resover = new UndoContextResolver();
        var manager = new UndoManager(resover);
        using var transaction = manager.CreateTransaction("add 4 lines");
        transaction.Add(new UndoCollectionAddOperation());
    }
}
