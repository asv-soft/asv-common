using System;
using System.IO;
using Xunit;

namespace Asv.IO.Test;

public partial class BinSerializeTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    [InlineData(133337)]
    [InlineData(-133337)]
    public void IntCanBeSerialized(int val)
    {
        var buffer = new byte[4];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WriteInt(ref writeSpan, val);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(val, BinSerialize.ReadInt(ref readSpan));
    }

    [Fact]
    public void IntCanBeReserved()
    {
        var buffer = new byte[4];
        var writeSpan = new Span<byte>(buffer);

        ref int reserved = ref BinSerialize.ReserveInt(ref writeSpan);
        reserved = -133337;

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(-133337, BinSerialize.ReadInt(ref readSpan));
    }

    [Theory]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    [InlineData(0)]
    [InlineData(1)]
    public void IntCanBeSerializedWithStream(int value)
    {
        using var stream = new MemoryStream();
        BinSerialize.WriteInt(stream, value);
        stream.Position = 0;
        var read = BinSerialize.ReadInt(stream);
        Assert.Equal(value, read);
    }
}
