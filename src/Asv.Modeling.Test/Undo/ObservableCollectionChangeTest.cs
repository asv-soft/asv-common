using System.Buffers;
using JetBrains.Annotations;
using ObservableCollections;
using R3;

namespace Asv.Modeling.Test;

[TestSubject(typeof(CollectionUndoChange<>))]
public class CollectionUndoChangeTest
{
    [Fact]
    public void Default_HasDefaultValues()
    {
        var change = new CollectionUndoChange<string>();

        Assert.Equal(ChangeOperation.Update, change.Operation);
        Assert.Equal(default, change.OldIndex);
        Assert.Equal(default, change.NewIndex);
        Assert.Null(change.OldValue);
        Assert.Null(change.NewValue);
    }

    [Fact]
    public void Properties_StoreAssignedValues()
    {
        var change = new CollectionUndoChange<string?>
        {
            Operation = ChangeOperation.Create,
            OldIndex = -1,
            NewIndex = 2,
            OldValue = null,
            NewValue = "created",
        };

        Assert.Equal(ChangeOperation.Create, change.Operation);
        Assert.Equal(-1, change.OldIndex);
        Assert.Equal(2, change.NewIndex);
        Assert.Null(change.OldValue);
        Assert.Equal("created", change.NewValue);
    }

    [Fact]
    public void SerializeDeserialize_RoundTripsValues()
    {
        var source = new CollectionUndoChange<string?>
        {
            Operation = ChangeOperation.Delete,
            OldIndex = 3,
            NewIndex = -1,
            OldValue = "removed",
            NewValue = null,
        };

        var writer = new ArrayBufferWriter<byte>();
        source.Serialize(writer);

        var actual = new CollectionUndoChange<string?>();
        actual.Deserialize(new ReadOnlySequence<byte>(writer.WrittenMemory));

        Assert.Equal(source.Operation, actual.Operation);
        Assert.Equal(source.OldIndex, actual.OldIndex);
        Assert.Equal(source.NewIndex, actual.NewIndex);
        Assert.True(source.OldItems.SequenceEqual(actual.OldItems));
        Assert.True(source.NewItems.SequenceEqual(actual.NewItems));
        Assert.Equal(source.OldValue, actual.OldValue);
        Assert.Equal(source.NewValue, actual.NewValue);
    }

    [Fact]
    public void SerializeDeserialize_RoundTripsBatchUpdate()
    {
        var source = new CollectionUndoChange<string>
        {
            Operation = ChangeOperation.Update,
            OldStartingIndex = 1,
            NewStartingIndex = 1,
            OldItems = ["second", "third"],
            NewItems = ["updated-second", "updated-third"],
        };

        var writer = new ArrayBufferWriter<byte>();
        source.Serialize(writer);

        var actual = new CollectionUndoChange<string>();
        actual.Deserialize(new ReadOnlySequence<byte>(writer.WrittenMemory));

        Assert.Equal(ChangeOperation.Update, actual.Operation);
        Assert.Equal(1, actual.OldStartingIndex);
        Assert.Equal(1, actual.NewStartingIndex);
        Assert.True(actual.OldItems.SequenceEqual(["second", "third"]));
        Assert.True(actual.NewItems.SequenceEqual(["updated-second", "updated-third"]));
        Assert.Equal("second", actual.OldValue);
        Assert.Equal("updated-second", actual.NewValue);
    }

    [Fact]
    public void ImplementsGenericChangeContract()
    {
        IUndoChange<int> undoChange = new CollectionUndoChange<int>
        {
            Operation = ChangeOperation.Update,
            OldIndex = 1,
            NewIndex = 1,
            OldValue = 10,
            NewValue = 20,
        };

        Assert.Equal(ChangeOperation.Update, undoChange.Operation);
        Assert.Equal(10, undoChange.OldValue);
        Assert.Equal(20, undoChange.NewValue);
    }

    [Fact]
    public void ObservableList_Add_MapsToCreateChange()
    {
        var list = new ObservableList<string>();
        var received = new List<CollectionUndoChange<string>>();
        void OnCollectionChanged(in NotifyCollectionChangedEventArgs<string> args)
        {
            received.Add(CollectionUndoChange<string>.From(args));
        }

        list.CollectionChanged += OnCollectionChanged;
        using var subscription = Disposable.Create(() =>
            list.CollectionChanged -= OnCollectionChanged
        );

        list.Add("created");

        var change = Assert.Single(received);
        Assert.Equal(ChangeOperation.Create, change.Operation);
        Assert.Equal(-1, change.OldIndex);
        Assert.Equal(0, change.NewIndex);
        Assert.Null(change.OldValue);
        Assert.Equal("created", change.NewValue);
        Assert.Empty(change.OldItems);
        Assert.True(change.NewItems.SequenceEqual(["created"]));
    }

    [Fact]
    public void ObservableList_Remove_MapsToDeleteChange()
    {
        var list = new ObservableList<string> { "first", "second" };
        var received = new List<CollectionUndoChange<string>>();
        void OnCollectionChanged(in NotifyCollectionChangedEventArgs<string> args)
        {
            received.Add(CollectionUndoChange<string>.From(args));
        }

        list.CollectionChanged += OnCollectionChanged;
        using var subscription = Disposable.Create(() =>
            list.CollectionChanged -= OnCollectionChanged
        );

        list.RemoveAt(1);

        var change = Assert.Single(received);
        Assert.Equal(ChangeOperation.Delete, change.Operation);
        Assert.Equal(1, change.OldIndex);
        Assert.Equal(-1, change.NewIndex);
        Assert.Equal("second", change.OldValue);
        Assert.Null(change.NewValue);
        Assert.True(change.OldItems.SequenceEqual(["second"]));
        Assert.Empty(change.NewItems);
    }

    [Fact]
    public void ObservableList_InsertRange_MapsToSingleCreateChange()
    {
        var list = new ObservableList<string> { "first" };
        var received = new List<CollectionUndoChange<string>>();
        void OnCollectionChanged(in NotifyCollectionChangedEventArgs<string> args)
        {
            received.Add(CollectionUndoChange<string>.From(args));
        }

        list.CollectionChanged += OnCollectionChanged;
        using var subscription = Disposable.Create(() =>
            list.CollectionChanged -= OnCollectionChanged
        );

        list.InsertRange(1, ["second", "third"]);

        var change = Assert.Single(received);
        Assert.Equal(ChangeOperation.Create, change.Operation);
        Assert.Equal(-1, change.OldStartingIndex);
        Assert.Equal(1, change.NewStartingIndex);
        Assert.Empty(change.OldItems);
        Assert.True(change.NewItems.SequenceEqual(["second", "third"]));
    }

    [Fact]
    public void BatchUpdate_StoresAllOldAndNewItems()
    {
        var change = new CollectionUndoChange<string>
        {
            Operation = ChangeOperation.Update,
            OldStartingIndex = 0,
            NewStartingIndex = 0,
            OldItems = ["a", "b", "c"],
            NewItems = ["A", "B", "C"],
        };

        Assert.Equal(ChangeOperation.Update, change.Operation);
        Assert.Equal(0, change.OldIndex);
        Assert.Equal(0, change.NewIndex);
        Assert.True(change.OldItems.SequenceEqual(["a", "b", "c"]));
        Assert.True(change.NewItems.SequenceEqual(["A", "B", "C"]));
        Assert.Equal("a", change.OldValue);
        Assert.Equal("A", change.NewValue);
    }
}
