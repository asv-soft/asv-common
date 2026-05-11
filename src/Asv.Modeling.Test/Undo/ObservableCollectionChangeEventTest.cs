using System.Buffers;
using JetBrains.Annotations;
using ObservableCollections;
using R3;

namespace Asv.Modeling.Test;

[TestSubject(typeof(ObservableCollectionChangeEvent<>))]
public class ObservableCollectionChangeEventTest
{
    [Fact]
    public void Default_HasDefaultValues()
    {
        var change = new ObservableCollectionChangeEvent<string>();

        Assert.Equal(ChangeOperation.Update, change.Operation);
        Assert.Equal(default, change.OldIndex);
        Assert.Equal(default, change.NewIndex);
        Assert.Null(change.OldValue);
        Assert.Null(change.NewValue);
    }

    [Fact]
    public void Properties_StoreAssignedValues()
    {
        var change = new ObservableCollectionChangeEvent<string?>
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
        var source = new ObservableCollectionChangeEvent<string?>
        {
            Operation = ChangeOperation.Delete,
            OldIndex = 3,
            NewIndex = -1,
            OldValue = "removed",
            NewValue = null,
        };

        var writer = new ArrayBufferWriter<byte>();
        source.Serialize(writer);

        var actual = new ObservableCollectionChangeEvent<string?>();
        actual.Deserialize(new ReadOnlySequence<byte>(writer.WrittenMemory));

        Assert.Equal(source.Operation, actual.Operation);
        Assert.Equal(source.OldIndex, actual.OldIndex);
        Assert.Equal(source.NewIndex, actual.NewIndex);
        Assert.Equal(source.OldValue, actual.OldValue);
        Assert.Equal(source.NewValue, actual.NewValue);
    }

    [Fact]
    public void ImplementsGenericChangeContract()
    {
        IChange<int> change = new ObservableCollectionChangeEvent<int>
        {
            Operation = ChangeOperation.Update,
            OldIndex = 1,
            NewIndex = 1,
            OldValue = 10,
            NewValue = 20,
        };

        Assert.Equal(ChangeOperation.Update, change.Operation);
        Assert.Equal(10, change.OldValue);
        Assert.Equal(20, change.NewValue);
    }

    [Fact]
    public void ObservableList_Add_MapsToCreateChange()
    {
        var list = new ObservableList<string>();
        var received = new List<ObservableCollectionChangeEvent<string?>>();
#pragma warning disable xUnit1051
        using var subscription = list.ObserveAdd()
            .Subscribe(x =>
                received.Add(
                    new ObservableCollectionChangeEvent<string?>
                    {
                        Operation = ChangeOperation.Create,
                        OldIndex = -1,
                        NewIndex = x.Index,
                        OldValue = default,
                        NewValue = x.Value,
                    }
                )
            );
#pragma warning restore xUnit1051

        list.Add("created");

        var change = Assert.Single(received);
        Assert.Equal(ChangeOperation.Create, change.Operation);
        Assert.Equal(-1, change.OldIndex);
        Assert.Equal(0, change.NewIndex);
        Assert.Null(change.OldValue);
        Assert.Equal("created", change.NewValue);
    }

    [Fact]
    public void ObservableList_Remove_MapsToDeleteChange()
    {
        var list = new ObservableList<string> { "first", "second" };
        var received = new List<ObservableCollectionChangeEvent<string?>>();
#pragma warning disable xUnit1051
        using var subscription = list.ObserveRemove()
            .Subscribe(x =>
                received.Add(
                    new ObservableCollectionChangeEvent<string?>
                    {
                        Operation = ChangeOperation.Delete,
                        OldIndex = x.Index,
                        NewIndex = -1,
                        OldValue = x.Value,
                        NewValue = default,
                    }
                )
            );
#pragma warning restore xUnit1051

        list.RemoveAt(1);

        var change = Assert.Single(received);
        Assert.Equal(ChangeOperation.Delete, change.Operation);
        Assert.Equal(1, change.OldIndex);
        Assert.Equal(-1, change.NewIndex);
        Assert.Equal("second", change.OldValue);
        Assert.Null(change.NewValue);
    }
}
