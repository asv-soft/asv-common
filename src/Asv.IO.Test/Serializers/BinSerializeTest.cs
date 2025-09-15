using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace Asv.IO.Test
{
    public partial class BinSerializeTest
    {
        [Fact]
        public void UnalignedWritesCanBeRead()
        {
            var buffer = new byte[64];
            var writeSpan = new Span<byte>(buffer);

            // Write 32 integers that are not aligned to 4 bytes.
            BinSerialize.WriteByte(ref writeSpan, 137);
            BinSerialize.WriteInt(ref writeSpan, 133337);
            BinSerialize.WriteByte(ref writeSpan, 137);
            BinSerialize.WriteInt(ref writeSpan, 133337);

            var readSpan = new ReadOnlySpan<byte>(buffer);
            Assert.Equal(137, BinSerialize.ReadByte(ref readSpan));
            Assert.Equal(133337, BinSerialize.ReadInt(ref readSpan));
            Assert.Equal(137, BinSerialize.ReadByte(ref readSpan));
            Assert.Equal(133337, BinSerialize.ReadInt(ref readSpan));
        }
    }
}
