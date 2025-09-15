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
    [InlineData(-133337)]
    [InlineData(-1337)]
    [InlineData(133337)]
    [InlineData(137)]
    public void PackedIntegerWriteCanBeEstimated(int val)
    {
        var expectedBytes = BinSerialize.GetSizeForPackedInteger(val);

        var buffer = new byte[64];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WritePackedInteger(ref writeSpan, val);
        var writtenBytes = buffer.Length - writeSpan.Length;

        Assert.Equal(writtenBytes, expectedBytes);
    }

    [Theory]
    [InlineData(-134217729, 5)]
    [InlineData(-134217728, 4)]
    [InlineData(-1048577, 4)]
    [InlineData(-1048576, 3)]
    [InlineData(-8193, 3)]
    [InlineData(-8192, 2)]
    [InlineData(-65, 2)]
    [InlineData(-64, 1)]
    [InlineData(0, 1)]
    [InlineData(63, 1)]
    [InlineData(64, 2)]
    [InlineData(8191, 2)]
    [InlineData(8192, 3)]
    [InlineData(1048575, 3)]
    [InlineData(1048576, 4)]
    [InlineData(134217727, 4)]
    [InlineData(134217728, 5)]
    public void PackedIntegerWriteIsExpectedSize(int val, int expectedSize) =>
        Assert.Equal(expectedSize, BinSerialize.GetSizeForPackedInteger(val));

    [Theory]
    [InlineData(int.MaxValue, 5)]
    [InlineData(int.MinValue, 5)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(63, 1)]
    [InlineData(64, 2)]
    [InlineData(8191, 2)]
    [InlineData(8192, 3)]
    [InlineData(1048575, 3)]
    [InlineData(1048576, 4)]
    [InlineData(134217727, 4)]
    [InlineData(-64, 1)]
    [InlineData(-65, 2)]
    [InlineData(-8192, 2)]
    [InlineData(-8193, 3)]
    [InlineData(-1048576, 3)]
    [InlineData(-1048577, 4)]
    [InlineData(-134217728, 4)]
    [InlineData(-134217729, 5)]
    public void PacketIntCanBeSerializedWithStream(int value, int size)
    {
        using var stream = new MemoryStream();
        BinSerialize.WritePackedInteger(stream, value);
        Assert.Equal(size, stream.Length);
        stream.Position = 0;
        var read = BinSerialize.ReadPackedInteger(stream);
        Assert.Equal(value, read);
    }

    [Theory]
    [InlineData(int.MaxValue, 5)]
    [InlineData(int.MinValue, 5)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(63, 1)]
    [InlineData(64, 2)]
    [InlineData(8191, 2)]
    [InlineData(8192, 3)]
    [InlineData(1048575, 3)]
    [InlineData(1048576, 4)]
    [InlineData(134217727, 4)]
    [InlineData(-64, 1)]
    [InlineData(-65, 2)]
    [InlineData(-8192, 2)]
    [InlineData(-8193, 3)]
    [InlineData(-1048576, 3)]
    [InlineData(-1048577, 4)]
    [InlineData(-134217728, 4)]
    [InlineData(-134217729, 5)]
    public void PackedIntegerCanBeSerializedWithSpan(int val, int size)
    {
        var buffer = new byte[size];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WritePackedInteger(ref writeSpan, val);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(val, BinSerialize.ReadPackedInteger(ref readSpan));
    }

    [Theory]
    [InlineData(int.MaxValue, 5)]
    [InlineData(int.MinValue, 5)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(63, 1)]
    [InlineData(64, 2)]
    [InlineData(8191, 2)]
    [InlineData(8192, 3)]
    [InlineData(1048575, 3)]
    [InlineData(1048576, 4)]
    [InlineData(134217727, 4)]
    [InlineData(-64, 1)]
    [InlineData(-65, 2)]
    [InlineData(-8192, 2)]
    [InlineData(-8193, 3)]
    [InlineData(-1048576, 3)]
    [InlineData(-1048577, 4)]
    [InlineData(-134217728, 4)]
    [InlineData(-134217729, 5)]
    public void PackedIntegerCanBeSerializedWithMemory(int val, int size)
    {
        var buffer = new byte[size];
        var writeSpan = new Memory<byte>(buffer);
        BinSerialize.WritePackedInteger(ref writeSpan, val);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(val, BinSerialize.ReadPackedInteger(ref readSpan));
    }
}
