using System;
using Xunit;

namespace Asv.IO.Test;

public partial class BinSerializeTest
{
    [Theory]
    [InlineData(byte.MinValue)]
    [InlineData(byte.MaxValue)]
    [InlineData(137)]
    public void ByteCanBeSerialized(byte val)
    {
        var buffer = new byte[1];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WriteByte(ref writeSpan, val);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(val, BinSerialize.ReadByte(ref readSpan));
    }

    [Fact]
    public void ByteCanBeReserved()
    {
        var buffer = new byte[1];
        var writeSpan = new Span<byte>(buffer);

        ref byte reserved = ref BinSerialize.ReserveByte(ref writeSpan);
        reserved = 137;

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(137, BinSerialize.ReadByte(ref readSpan));
    }
}
