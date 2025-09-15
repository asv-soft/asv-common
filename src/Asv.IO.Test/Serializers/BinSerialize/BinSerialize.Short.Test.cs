using System;
using Xunit;

namespace Asv.IO.Test;

public partial class BinSerializeTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(short.MinValue)]
    [InlineData(short.MaxValue)]
    [InlineData(1337)]
    [InlineData(-1337)]
    public void ShortCanBeSerialized(short val)
    {
        var buffer = new byte[2];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WriteShort(ref writeSpan, val);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(val, BinSerialize.ReadShort(ref readSpan));
    }

    [Fact]
    public void ShortCanBeReserved()
    {
        var buffer = new byte[2];
        var writeSpan = new Span<byte>(buffer);

        ref short reserved = ref BinSerialize.ReserveShort(ref writeSpan);
        reserved = -1337;

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(-1337, BinSerialize.ReadShort(ref readSpan));
    }
}
