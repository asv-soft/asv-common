using System;
using Xunit;

namespace Asv.IO.Test;

public partial class BinSerializeTest
{
    [Theory]
    [InlineData(uint.MinValue)]
    [InlineData(uint.MaxValue)]
    [InlineData((uint)int.MaxValue + 1337)]
    public void PackedUnsignedIntegerCanBeSerialized(uint val)
    {
        var buffer = new byte[5];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WritePackedUnsignedInteger(ref writeSpan, val);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(val, BinSerialize.ReadPackedUnsignedInteger(ref readSpan));
    }

    [Theory]
    [InlineData(uint.MinValue)]
    [InlineData(uint.MaxValue)]
    [InlineData((uint)int.MaxValue + 1337)]
    public void PackedUnsignedIntegerWriteCanBeEstimated(uint val)
    {
        var expectedBytes = BinSerialize.GetSizeForPackedUnsignedInteger(val);

        var buffer = new byte[64];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WritePackedUnsignedInteger(ref writeSpan, val);
        var writtenBytes = buffer.Length - writeSpan.Length;

        Assert.Equal(writtenBytes, expectedBytes);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(127, 1)]
    [InlineData(128, 2)]
    [InlineData(16383, 2)]
    [InlineData(16384, 3)]
    [InlineData(2097151, 3)]
    [InlineData(2097152, 4)]
    [InlineData(268435455, 4)]
    [InlineData(268435456, 5)]
    public void PackedUnsignedIntegerWriteIsExpectedSize(uint val, int expectedSize) =>
        Assert.Equal(expectedSize, BinSerialize.GetSizeForPackedUnsignedInteger(val));
}
