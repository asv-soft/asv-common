using System;
using Xunit;

namespace Asv.IO.Test;

public partial class BinSerializeTest
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BoolCanBeSerialized(bool val)
    {
        var buffer = new byte[1];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WriteBool(ref writeSpan, val);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(val, BinSerialize.ReadBool(ref readSpan));
    }

    [Fact]
    public void BoolCanBeReserved()
    {
        var buffer = new byte[1];
        var writeSpan = new Span<byte>(buffer);

        ref bool reserved = ref BinSerialize.ReserveBool(ref writeSpan);
        reserved = true;

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.True(BinSerialize.ReadBool(ref readSpan));
    }
}
