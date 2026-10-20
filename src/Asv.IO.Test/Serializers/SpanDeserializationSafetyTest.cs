using System;
using System.Buffers;
using Xunit;

namespace Asv.IO.Test;

public class SpanDeserializationSafetyTest
{
    [Fact]
    public void ArrayDeserialize_ReusedInstance_ReplacesPreviousValueAndSupportsEmpty()
    {
        var value = new SpanByteArrayType();
        value.Deserialize(new byte[] { 2, 41, 42 });
        Assert.Equal(new byte[] { 41, 42 }, value.Items);

        value.Deserialize(new byte[] { 1, 7 });
        Assert.Equal(new byte[] { 7 }, value.Items);
        Assert.Equal(new byte[] { 1, 7 }, value.Serialize());

        value.Deserialize(new byte[] { 0 });
        Assert.Empty(value.Items);
        Assert.Equal(new byte[] { 0 }, value.Serialize());
    }

    [Fact]
    public void ArrayDeserialize_IncompleteValue_DoesNotRetainPreviousItems()
    {
        var value = new SpanByteArrayType([41, 42]);
        Assert.Throws<IndexOutOfRangeException>(() => value.Deserialize(new byte[] { 2, 7 }));
        Assert.Equal(new byte[] { 7 }, value.Items);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SequenceDeserialize_ConsumesOnlyOneValueAndPreservesIncompleteInput(bool segmented)
    {
        var bytes = new byte[] { 11, 22, 33 };
        var sequence = segmented
            ? TestSequence.Create(bytes.AsMemory(0, 1), bytes.AsMemory(1))
            : new ReadOnlySequence<byte>(bytes);
        var value = new SpanDoubleByteType();

        Assert.True(value.Deserialize(ref sequence));
        Assert.Equal(11, value.Value1);
        Assert.Equal(22, value.Value2);
        Assert.Equal(new byte[] { 33 }, sequence.ToArray());

        Assert.False(value.Deserialize(ref sequence));
        Assert.Equal(new byte[] { 33 }, sequence.ToArray());
        Assert.Equal(11, value.Value1);
        Assert.Equal(22, value.Value2);
    }
}
