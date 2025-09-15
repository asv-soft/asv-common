using System;
using Xunit;

namespace Asv.IO.Test;

public partial class BinSerializeTest
{
    [Fact]
    public void BlockCanBeSerialized()
    {
        // Get random bytes to serialize.
        var random = new Random(Seed: 1337);
        var data = new byte[64];
        random.NextBytes(data);

        // Write the data.
        var buffer = new byte[128];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WriteBlock(ref writeSpan, data);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        var readBlock = BinSerialize.ReadBlock(ref readSpan, byteCount: data.Length);

        Assert.NotNull(readBlock);
        Assert.Equal(data.Length, readBlock.Length);
        Assert.True(data.AsSpan().SequenceEqual(readBlock.AsSpan()));
    }
}
