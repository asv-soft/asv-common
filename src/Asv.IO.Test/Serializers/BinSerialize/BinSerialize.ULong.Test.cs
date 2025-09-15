using System;
using Xunit;

namespace Asv.IO.Test;

public partial class BinSerializeTest
{
    [Theory]
    [InlineData(ulong.MinValue)]
    [InlineData(ulong.MaxValue)]
    [InlineData(13333337)]
    public void ULongCanBeSerialized(ulong val)
    {
        var buffer = new byte[8];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WriteULong(ref writeSpan, val);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(val, BinSerialize.ReadULong(ref readSpan));
    }

    [Fact]
    public void ULongCanBeReserved()
    {
        var buffer = new byte[8];
        var writeSpan = new Span<byte>(buffer);

        ref ulong reserved = ref BinSerialize.ReserveULong(ref writeSpan);
        reserved = 1333333337;

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(1333333337, BinSerialize.ReadLong(ref readSpan));
    }
}
