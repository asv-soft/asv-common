using System;
using System.IO;
using Xunit;

namespace Asv.IO.Test;

public partial class BinSerializeTest
{
    [Theory]
    [InlineData(0f, 1f, 0f, 0f)]
    [InlineData(0f, 1f, 1f, 1f)]
    [InlineData(0f, 1f, -.1f, 0f)]
    [InlineData(0f, 1f, 1.1f, 1f)]
    [InlineData(-1f, 1f, -1.1f, -1f)]
    [InlineData(0f, 255f, 128f, 128f)]
    [InlineData(0f, 255f, 255f, 255f)]
    [InlineData(50f, 100f, 75f, 75.1f)] // 75.1f due to 8-bit precision quantization.
    [InlineData(-1f, 1f, 0f, 0f)]
    [InlineData(0f, 1f, .25f, .25f)]
    public void Write8BitRange_ShouldRoundTripWithinPrecision_WhenReadBack(
        float min,
        float max,
        float val,
        float expectedVal
    )
    {
        var buffer = new byte[1];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.Write8BitRange(ref writeSpan, min, max, val);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(expectedVal, BinSerialize.Read8BitRange(ref readSpan, min, max), precision: 2);
    }

    [Theory]
    [InlineData(0f, 1f, 0f, 0f)]
    [InlineData(0f, 1f, 1f, 1f)]
    [InlineData(0f, 1f, -.1f, 0f)]
    [InlineData(0f, 1f, 1.1f, 1f)]
    [InlineData(-1f, 1f, -1.1f, -1f)]
    [InlineData(0f, 65_535f, 32_767f, 32_767f)]
    [InlineData(0f, 65_535f, 65_535f, 65_535f)]
    [InlineData(-1f, 1f, 0f, 0f)]
    [InlineData(0f, 1f, .25f, .25f)]
    public void Write16BitRange_ShouldRoundTripWithinPrecision_WhenReadBack(
        float min,
        float max,
        float val,
        float expectedVal
    )
    {
        var buffer = new byte[2];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.Write16BitRange(ref writeSpan, min, max, val);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(
            expectedVal,
            BinSerialize.Read16BitRange(ref readSpan, min, max),
            precision: 4
        );
    }

    private static float Expected(float min, float max, byte raw) =>
        min + ((max - min) * (raw / (float)byte.MaxValue));

    [Theory]
    [InlineData(0f, 100f, 0)]
    [InlineData(-50f, 50f, 0)]
    [InlineData(0f, 100f, 255)]
    [InlineData(-50f, 50f, 255)]
    [InlineData(0f, 1f, 128)]
    [InlineData(-1f, 1f, 128)]
    [InlineData(10f, 20f, 64)]
    public void Read8BitRange_ShouldReturnInterpolatedValue_GivenRawByte(
        float min,
        float max,
        int rawInt
    )
    {
        var raw = (byte)rawInt;
        using var ms = new MemoryStream(new[] { raw });

        var actual = BinSerialize.Read8BitRange(ms, in min, in max);
        var expected = Expected(min, max, raw);

        var eps = 1e-6f;
        Assert.InRange(actual, expected - eps, expected + eps);
    }

    [Fact]
    public void Read8BitRange_ShouldAdvanceStreamPositionByOneByte()
    {
        using var ms = new MemoryStream("{-"u8.ToArray()); // two bytes
        var startPos = ms.Position;

        var min = 0f;
        var max = 10f;
        _ = BinSerialize.Read8BitRange(ms, in min, in max);

        Assert.Equal(startPos + 1, ms.Position);
    }

    [Fact]
    public void Read8BitRange_ShouldThrowEndOfStreamException_WhenStreamIsEmpty()
    {
        using var ms = new MemoryStream([]);

        Assert.Throws<EndOfStreamException>(() =>
        {
            var min = 0f;
            var max = 1f;
            _ = BinSerialize.Read8BitRange(ms, in min, in max);
        });
    }

    [Theory]
    [InlineData(100f, 0f, 0)]
    [InlineData(100f, 0f, 255)]
    [InlineData(100f, 0f, 128)]
    public void Read8BitRange_ShouldHandleDescendingRange(float min, float max, int rawInt)
    {
        var raw = (byte)rawInt;
        using var ms = new MemoryStream(new[] { raw });

        var actual = BinSerialize.Read8BitRange(ms, in min, in max);
        var expected = Expected(min, max, raw);

        var eps = 1e-6f;
        Assert.InRange(actual, expected - eps, expected + eps);
    }
}
