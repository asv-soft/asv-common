using JetBrains.Annotations;

namespace Asv.Modeling.Test;

[TestSubject(typeof(UndoChangeSinkMixin))]
public class UndoChangeSinkMixinTest
{
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

    private sealed class TestUndoChangeSink<T> : IUndoChangeSink<UndoChange<T>>
    {
        public List<UndoChange<T>> Changes { get; } = [];

        public void Publish(UndoChange<T> change)
        {
            Changes.Add(change);
        }

        public void Dispose() { }
    }
}
