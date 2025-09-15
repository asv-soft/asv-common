using System;
using Xunit;

namespace Asv.IO.Test;

public partial class BinSerializeTest
{
    [Theory]
    [InlineData(uint.MinValue)]
    [InlineData(uint.MaxValue)]
    [InlineData(133337)]
    public void UIntCanBeSerialized(uint val)
    {
        var buffer = new byte[4];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WriteUInt(ref writeSpan, val);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(val, BinSerialize.ReadUInt(ref readSpan));
    }

    [Fact]
    public void UIntCanBeReserved()
    {
        var buffer = new byte[4];
        var writeSpan = new Span<byte>(buffer);

        ref uint reserved = ref BinSerialize.ReserveUInt(ref writeSpan);
        reserved = 133337;

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal((uint)133337, BinSerialize.ReadUInt(ref readSpan));
    }
}
