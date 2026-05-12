using System.Buffers;
using JetBrains.Annotations;

namespace Asv.Modeling.Test;

[TestSubject(typeof(KeyValueUndoChange<,>))]
public class KeyValueUndoChangeTest
{
    [Fact]
    public void Default_HasDefaultValues()
    {
        var change = new KeyValueUndoChange<string, int>();

        Assert.Equal(ChangeOperation.Update, change.Operation);
        Assert.Null(change.Key);
        Assert.Equal(default, change.OldValue);
        Assert.Equal(default, change.NewValue);
    }

    [Fact]
    public void Properties_StoreAssignedValues()
    {
        var change = new KeyValueUndoChange<string, int>
        {
            Operation = ChangeOperation.Delete,
            Key = "speed",
            OldValue = 10,
            NewValue = 20,
        };

        Assert.Equal(ChangeOperation.Delete, change.Operation);
        Assert.Equal("speed", change.Key);
        Assert.Equal(10, change.OldValue);
        Assert.Equal(20, change.NewValue);
    }

    [Fact]
    public void SerializeDeserialize_RoundTripsValues()
    {
        var source = new KeyValueUndoChange<string, int>
        {
            Operation = ChangeOperation.Update,
            Key = "altitude",
            OldValue = 100,
            NewValue = 250,
        };

        var actual = SerializeAndDeserialize(source);

        Assert.Equal(source.Key, actual.Key);
        Assert.Equal(source.Operation, actual.Operation);
        Assert.Equal(source.OldValue, actual.OldValue);
        Assert.Equal(source.NewValue, actual.NewValue);
    }

    [Fact]
    public void SerializeDeserialize_RoundTripsNullValue()
    {
        var source = new KeyValueUndoChange<string, string?>
        {
            Operation = ChangeOperation.Create,
            Key = "name",
            OldValue = null,
            NewValue = "updated",
        };

        var actual = SerializeAndDeserialize(source);

        Assert.Equal(source.Key, actual.Key);
        Assert.Equal(source.Operation, actual.Operation);
        Assert.Null(actual.OldValue);
        Assert.Equal(source.NewValue, actual.NewValue);
    }

    [Fact]
    public void Deserialize_OverwritesExistingValues()
    {
        var source = new KeyValueUndoChange<string, int>
        {
            Operation = ChangeOperation.Delete,
            Key = "target",
            OldValue = 1,
            NewValue = 2,
        };
        var writer = new ArrayBufferWriter<byte>();
        source.Serialize(writer);

        var actual = new KeyValueUndoChange<string, int>
        {
            Operation = ChangeOperation.Create,
            Key = "other",
            OldValue = 100,
            NewValue = 200,
        };

        actual.Deserialize(new ReadOnlySequence<byte>(writer.WrittenMemory));

        Assert.Equal(source.Key, actual.Key);
        Assert.Equal(source.Operation, actual.Operation);
        Assert.Equal(source.OldValue, actual.OldValue);
        Assert.Equal(source.NewValue, actual.NewValue);
    }

    [Fact]
    public void ScalarChange_ImplementsGenericChangeContract()
    {
        IValueUndoChange<int> undoChange = new ValueUndoChange<int>
        {
            Operation = ChangeOperation.Update,
            OldValue = 1,
            NewValue = 2,
        };

        Assert.Equal(ChangeOperation.Update, undoChange.Operation);
        Assert.Equal(1, undoChange.OldValue);
        Assert.Equal(2, undoChange.NewValue);
    }

    [Fact]
    public void UndoChange_SerializeDeserialize_RoundTripsValues()
    {
        var source = new ValueUndoChange<string>
        {
            Operation = ChangeOperation.Update,
            OldValue = "old",
            NewValue = "new",
        };
        var writer = new ArrayBufferWriter<byte>();
        source.Serialize(writer);

        var actual = new ValueUndoChange<string>();
        actual.Deserialize(new ReadOnlySequence<byte>(writer.WrittenMemory));

        Assert.Equal(source.Operation, actual.Operation);
        Assert.Equal(source.OldValue, actual.OldValue);
        Assert.Equal(source.NewValue, actual.NewValue);
    }

    private static KeyValueUndoChange<TKey, TValue> SerializeAndDeserialize<TKey, TValue>(
        KeyValueUndoChange<TKey, TValue> source
    )
    {
        var writer = new ArrayBufferWriter<byte>();
        source.Serialize(writer);

        var actual = new KeyValueUndoChange<TKey, TValue>();
        actual.Deserialize(new ReadOnlySequence<byte>(writer.WrittenMemory));
        return actual;
    }
}
