using System;
using System.IO;
using Xunit;

namespace Asv.IO.Test;

public partial class BinSerializeTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(sbyte.MinValue)]
    [InlineData(sbyte.MaxValue)]
    [InlineData(13)]
    [InlineData(-13)]
    public void SByteCanBeSerialized(sbyte val)
    {
        var buffer = new byte[1];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WriteSByte(ref writeSpan, val);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(val, BinSerialize.ReadSByte(ref readSpan));
    }

    [Fact]
    public void SByteCanBeReserved()
    {
        var buffer = new byte[1];
        var writeSpan = new Span<byte>(buffer);

        ref sbyte reserved = ref BinSerialize.ReserveSByte(ref writeSpan);
        reserved = -17;

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(-17, BinSerialize.ReadSByte(ref readSpan));
    }

    [Theory]
    [InlineData(sbyte.MaxValue)]
    [InlineData(sbyte.MinValue)]
    [InlineData(0)]
    [InlineData(1)]
    public void SByteCanBeSerializedWithStream(sbyte val)
    {
        using var stream = new MemoryStream();
        BinSerialize.WriteSByte(stream, val);
        stream.Position = 0;
        var read = BinSerialize.ReadSByte(stream);
        Assert.Equal(val, read);
    }
}
