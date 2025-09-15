using System;
using System.IO;
using System.Text;
using Xunit;

namespace Asv.IO.Test;

public partial class BinSerializeTest
{
    [Theory]
    [InlineData("")]
    [InlineData("Test String")]
    [InlineData("🇯🇵 🇰🇷 🇩🇪 🇨🇳 🇺🇸 🇫🇷 🇪🇸 🇮🇹 🇷🇺 🇬🇧")]
    [InlineData("Test\nString\n")]
    public void StringCanBeSerialized(string val)
    {
        var buffer = new byte[128];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WriteString(ref writeSpan, val);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(val, BinSerialize.ReadString(ref readSpan));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Test String")]
    [InlineData("🇯🇵 🇰🇷 🇩🇪 🇨🇳 🇺🇸 🇫🇷 🇪🇸 🇮🇹 🇷🇺 🇬🇧")]
    [InlineData("Test\nString\n")]
    public void StringWriteCanBeEstimated(string val)
    {
        var expectedBytes = BinSerialize.GetSizeForString(val);

        var buffer = new byte[128];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WriteString(ref writeSpan, val);
        var writtenBytes = buffer.Length - writeSpan.Length;

        Assert.Equal(writtenBytes, expectedBytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Test String")]
    [InlineData("🇯🇵 🇰🇷 🇩🇪 🇨🇳 🇺🇸 🇫🇷 🇪🇸 🇮🇹 🇷🇺 🇬🇧")]
    [InlineData("Test\nString\n")]
    public void StringFormatMatchesBinaryWriter(string val)
    {
        var memStreamBuffer = new byte[128];
        int memStreamBytesWritten;
        using (var memStream = new MemoryStream())
        {
            using (var binaryWriter = new BinaryWriter(memStream, Encoding.UTF8, leaveOpen: true))
            {
                binaryWriter.Write(val);
            }

            memStreamBytesWritten = (int)memStream.Length;
        }

        var buffer = new byte[128];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WriteString(ref writeSpan, val);
        var writtenBytes = buffer.Length - writeSpan.Length;

        Assert.Equal(memStreamBytesWritten, writtenBytes);
        Assert.True(
            memStreamBuffer
                .AsSpan()
                .Slice(memStreamBytesWritten)
                .SequenceEqual(buffer.AsSpan().Slice(writtenBytes))
        );
    }
}
