using System;
using Xunit;

namespace Asv.IO.Test;

public partial class BinSerializeTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(double.MinValue)]
    [InlineData(double.MaxValue)]
    [InlineData(1337.0023)]
    [InlineData(-1337.2323)]
    public void DoubleCanBeSerialized(double val)
    {
        var buffer = new byte[8];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WriteDouble(ref writeSpan, val);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(val, BinSerialize.ReadDouble(ref readSpan));
    }
}
