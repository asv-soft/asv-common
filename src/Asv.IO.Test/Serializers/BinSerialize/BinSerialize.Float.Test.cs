using System;
using Xunit;

namespace Asv.IO.Test;

public partial class BinSerializeTest
{
    [Theory]
    [InlineData(0f)]
    [InlineData(float.MinValue)]
    [InlineData(float.MaxValue)]
    [InlineData(1337.23f)]
    [InlineData(-1337.62f)]
    public void FloatCanBeSerialized(float val)
    {
        var buffer = new byte[4];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WriteFloat(ref writeSpan, val);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(val, BinSerialize.ReadFloat(ref readSpan));
    }
}
