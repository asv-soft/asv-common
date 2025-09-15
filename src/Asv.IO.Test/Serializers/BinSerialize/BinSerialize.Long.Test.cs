using System;
using Xunit;

namespace Asv.IO.Test;

public partial class BinSerializeTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    [InlineData(13333337)]
    [InlineData(-13333337)]
    public void LongCanBeSerialized(long val)
    {
        var buffer = new byte[8];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WriteLong(ref writeSpan, val);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(val, BinSerialize.ReadLong(ref readSpan));
    }

    [Fact]
    public void LongCanBeReserved()
    {
        var buffer = new byte[8];
        var writeSpan = new Span<byte>(buffer);

        ref long reserved = ref BinSerialize.ReserveLong(ref writeSpan);
        reserved = -1333333337;

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(-1333333337, BinSerialize.ReadLong(ref readSpan));
    }
}
