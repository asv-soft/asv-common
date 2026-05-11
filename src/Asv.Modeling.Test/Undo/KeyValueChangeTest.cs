using System.Buffers;
using JetBrains.Annotations;

namespace Asv.Modeling.Test;

[TestSubject(typeof(KeyValueChange<,>))]
public class KeyValueChangeTest
{
    [Fact]
    public void Default_HasDefaultValues()
    {
        var change = new KeyValueChange<string, int>();

        Assert.Equal(ChangeOperation.Update, change.Operation);
        Assert.Null(change.Key);
        Assert.Equal(default, change.OldValue);
        Assert.Equal(default, change.NewValue);
    }

    [Fact]
    public void Properties_StoreAssignedValues()
    {
        var change = new KeyValueChange<string, int>
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
        var source = new KeyValueChange<string, int>
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
        var source = new KeyValueChange<string, string?>
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
        var source = new KeyValueChange<string, int>
        {
            Operation = ChangeOperation.Delete,
            Key = "target",
            OldValue = 1,
            NewValue = 2,
        };
        var writer = new ArrayBufferWriter<byte>();
        source.Serialize(writer);

        var actual = new KeyValueChange<string, int>
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
        IChange<int> change = new Change<int>
        {
            Operation = ChangeOperation.Update,
            OldValue = 1,
            NewValue = 2,
        };

        Assert.Equal(ChangeOperation.Update, change.Operation);
        Assert.Equal(1, change.OldValue);
        Assert.Equal(2, change.NewValue);
    }

    private static KeyValueChange<TKey, TValue> SerializeAndDeserialize<TKey, TValue>(
        KeyValueChange<TKey, TValue> source
    )
    {
        var writer = new ArrayBufferWriter<byte>();
        source.Serialize(writer);

        var actual = new KeyValueChange<TKey, TValue>();
        actual.Deserialize(new ReadOnlySequence<byte>(writer.WrittenMemory));
        return actual;
    }
}
