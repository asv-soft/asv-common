using JetBrains.Annotations;

namespace Asv.Modeling.Test;

[TestSubject(typeof(ValueUndoChangeMixin))]
public class ValueUndoChangeMixinTest
{
    [Fact]
    public async ValueTask CreateValueChange_WithAsyncCallbacks_UsesOldValueForUndoAndNewValueForRedo()
    {
        using var controller = new UndoController<IViewModel>(new EmptyViewModel("root"));
        string? undoValue = null;
        string? redoValue = null;

        var sink = controller.CreateValueChange<string>(
            "value",
            (value, _) =>
            {
                undoValue = value;
                return ValueTask.CompletedTask;
            },
            (value, _) =>
            {
                redoValue = value;
                return ValueTask.CompletedTask;
            }
        );

        sink.Publish("old", "new");

        var handler = controller["value"];
        var change = new ValueUndoChange<string>
        {
            Operation = ChangeOperation.Update,
            OldValue = "old",
            NewValue = "new",
        };

        await handler.Undo(change, TestContext.Current.CancellationToken);
        await handler.Redo(change, TestContext.Current.CancellationToken);

        Assert.Equal("old", undoValue);
        Assert.Equal("new", redoValue);
    }

    [Fact]
    public async ValueTask CreateValueChange_WithSyncCallbacks_UsesOldValueForUndoAndNewValueForRedo()
    {
        using var controller = new UndoController<IViewModel>(new EmptyViewModel("root"));
        var undoValue = 0;
        var redoValue = 0;

        var sink = controller.CreateValueChange<int>(
            "value",
            value => undoValue = value,
            value => redoValue = value
        );

        sink.Publish((1, 2));

        var handler = controller["value"];
        var change = new ValueUndoChange<int>
        {
            Operation = ChangeOperation.Update,
            OldValue = 1,
            NewValue = 2,
        };

        await handler.Undo(change, TestContext.Current.CancellationToken);
        await handler.Redo(change, TestContext.Current.CancellationToken);

        Assert.Equal(1, undoValue);
        Assert.Equal(2, redoValue);
    }

    [Fact]
    public async ValueTask Create_WithSyncUndoCallbacks_RegistersHandler()
    {
        using var controller = new UndoController<IViewModel>(new EmptyViewModel("root"));
        ValueUndoChange<int>? undoChange = null;
        ValueUndoChange<int>? redoChange = null;

        controller.Register<ValueUndoChange<int>>(
            "value",
            change => undoChange = change,
            change => redoChange = change
        );

        var change = new ValueUndoChange<int>
        {
            Operation = ChangeOperation.Update,
            OldValue = 1,
            NewValue = 2,
        };
        var handler = controller["value"];

        await handler.Undo(change, TestContext.Current.CancellationToken);
        await handler.Redo(change, TestContext.Current.CancellationToken);

        Assert.Equal(1, undoChange?.OldValue);
        Assert.Equal(2, redoChange?.NewValue);
    }

    [Fact]
    public void Publish_WithOldAndNewValues_PublishesUpdateChange()
    {
        var sink = new TestUndoChangeSink<int>();

        sink.Publish(1, 2);

        var change = Assert.Single(sink.Changes);
        Assert.Equal(ChangeOperation.Update, change.Operation);
        Assert.Equal(1, change.OldValue);
        Assert.Equal(2, change.NewValue);
    }

    [Fact]
    public void Publish_WithEqualOldAndNewValues_DoesNotPublishUpdateChange()
    {
        var sink = new TestUndoChangeSink<int>();

        sink.Publish(1, 1);

        Assert.Empty(sink.Changes);
    }

    [Fact]
    public void Publish_WithOperationAndValues_PublishesChange()
    {
        var sink = new TestUndoChangeSink<string>();

        sink.Publish(ChangeOperation.Create, "", "created");

        var change = Assert.Single(sink.Changes);
        Assert.Equal(ChangeOperation.Create, change.Operation);
        Assert.Equal("", change.OldValue);
        Assert.Equal("created", change.NewValue);
    }

    [Fact]
    public void Publish_WithTuple_PublishesUpdateChange()
    {
        var sink = new TestUndoChangeSink<int>();

        sink.Publish((10, 20));

        var change = Assert.Single(sink.Changes);
        Assert.Equal(ChangeOperation.Update, change.Operation);
        Assert.Equal(10, change.OldValue);
        Assert.Equal(20, change.NewValue);
    }

    [Fact]
    public void Publish_WithOperationAndTuple_PublishesChange()
    {
        var sink = new TestUndoChangeSink<int>();

        sink.Publish(ChangeOperation.Delete, (10, 0));

        var change = Assert.Single(sink.Changes);
        Assert.Equal(ChangeOperation.Delete, change.Operation);
        Assert.Equal(10, change.OldValue);
        Assert.Equal(0, change.NewValue);
    }

    private sealed class TestUndoChangeSink<T> : IUndoChangeSink<ValueUndoChange<T>>
    {
        private int _suppressChangePublicationCount;

        public List<ValueUndoChange<T>> Changes { get; } = [];

        public IDisposable SuppressChangePublication()
        {
            Interlocked.Increment(ref _suppressChangePublicationCount);
            return R3.Disposable.Create(
                this,
                static sink => Interlocked.Decrement(ref sink._suppressChangePublicationCount)
            );
        }

        public void Publish(ValueUndoChange<T> change)
        {
            if (_suppressChangePublicationCount > 0)
                return;
            Changes.Add(change);
        }

        public void Dispose() { }
    }

    private sealed class EmptyViewModel(string id) : ViewModelBase(id)
    {
        public override IEnumerable<IViewModel> GetChildren()
        {
            return [];
        }
    }
}
