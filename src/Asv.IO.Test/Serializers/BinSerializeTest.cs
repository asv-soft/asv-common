using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace Asv.IO.Test
{
    public class BinSerializeTest
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

        [Theory]
        [InlineData(0)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        [InlineData(-1337)]
        [InlineData(-137)]
        [InlineData(1337)]
        [InlineData(137)]
        public void PackedIntegerCanBeSerialized(int val)
        {
            var buffer = new byte[5];
            var writeSpan = new Span<byte>(buffer);
            BinSerialize.WritePackedInteger(ref writeSpan, val);

            var readSpan = new ReadOnlySpan<byte>(buffer);
            Assert.Equal(val, BinSerialize.ReadPackedInteger(ref readSpan));
        }

        [Theory]
        [InlineData(uint.MinValue)]
        [InlineData(uint.MaxValue)]
        [InlineData((uint)int.MaxValue + 1337)]
        public void PackedUnsignedIntegerCanBeSerialized(uint val)
        {
            var buffer = new byte[5];
            var writeSpan = new Span<byte>(buffer);
            BinSerialize.WritePackedUnsignedInteger(ref writeSpan, val);

            var readSpan = new ReadOnlySpan<byte>(buffer);
            Assert.Equal(val, BinSerialize.ReadPackedUnsignedInteger(ref readSpan));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BoolCanBeSerialized(bool val)
        {
            var buffer = new byte[1];
            var writeSpan = new Span<byte>(buffer);
            BinSerialize.WriteBool(ref writeSpan, val);

            var readSpan = new ReadOnlySpan<byte>(buffer);
            Assert.Equal(val, BinSerialize.ReadBool(ref readSpan));
        }

        [Theory]
        [InlineData(byte.MinValue)]
        [InlineData(byte.MaxValue)]
        [InlineData(137)]
        public void ByteCanBeSerialized(byte val)
        {
            var buffer = new byte[1];
            var writeSpan = new Span<byte>(buffer);
            BinSerialize.WriteByte(ref writeSpan, val);

            var readSpan = new ReadOnlySpan<byte>(buffer);
            Assert.Equal(val, BinSerialize.ReadByte(ref readSpan));
        }

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

        [Theory]
        [InlineData(ushort.MinValue)]
        [InlineData(ushort.MaxValue)]
        [InlineData(1337)]
        public void UShortCanBeSerialized(ushort val)
        {
            var buffer = new byte[2];
            var writeSpan = new Span<byte>(buffer);
            BinSerialize.WriteUShort(ref writeSpan, val);

            var readSpan = new ReadOnlySpan<byte>(buffer);
            Assert.Equal(val, BinSerialize.ReadUShort(ref readSpan));
        }

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

        [Theory]
        [InlineData(uint.MinValue)]
        [InlineData(uint.MaxValue)]
        [InlineData(133337)]
        public void UIntCanBeSerialized(uint val)
        {
            var buffer = new byte[4];
            var writeSpan = new Span<byte>(buffer);
            BinSerialize.WriteUInt(ref writeSpan, val);

            var readSpan = new ReadOnlySpan<byte>(buffer);
            Assert.Equal(val, BinSerialize.ReadUInt(ref readSpan));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(long.MinValue)]
        [InlineData(long.MaxValue)]
        [InlineData(13333337)]
        [InlineData(-13333337)]
        public void LongCanBeSerialized(long val)
        {
            var buffer = new byte[8];
            var writeSpan = new Span<byte>(buffer);
            BinSerialize.WriteLong(ref writeSpan, val);

            var readSpan = new ReadOnlySpan<byte>(buffer);
            Assert.Equal(val, BinSerialize.ReadLong(ref readSpan));
        }

        [Theory]
        [InlineData(ulong.MinValue)]
        [InlineData(ulong.MaxValue)]
        [InlineData(13333337)]
        public void ULongCanBeSerialized(ulong val)
        {
            var buffer = new byte[8];
            var writeSpan = new Span<byte>(buffer);
            BinSerialize.WriteULong(ref writeSpan, val);

            var readSpan = new ReadOnlySpan<byte>(buffer);
            Assert.Equal(val, BinSerialize.ReadULong(ref readSpan));
        }

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

        [Theory]
        [InlineData(0f, 1f, 0f, 0f)]
        [InlineData(0f, 1f, 1f, 1f)]
        [InlineData(0f, 1f, -.1f, 0f)]
        [InlineData(0f, 1f, 1.1f, 1f)]
        [InlineData(-1f, 1f, -1.1f, -1f)]
        [InlineData(0f, 255f, 128f, 128f)]
        [InlineData(0f, 255f, 255f, 255f)]
        [InlineData(50f, 100f, 75f, 75.1f)] // 75.1f, due to only having 8 bit precision.
        [InlineData(-1f, 1f, 0f, 0f)]
        [InlineData(0f, 1f, .25f, .25f)]
        public void _8BitRangeCanBeSerialized(float min, float max, float val, float expectedVal)
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
        public void _16BitRangeCanBeSerialized(float min, float max, float val, float expectedVal)
        {
            var buffer = new byte[2];
            var writeSpan = new Span<byte>(buffer);
            BinSerialize.Write16BitRange(ref writeSpan, min, max, val);

            var readSpan = new ReadOnlySpan<byte>(buffer);
            Assert.Equal(expectedVal, BinSerialize.Read16BitRange(ref readSpan, min, max), precision: 4);
        }

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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TestStruct
        {
            public int A;
            public float B;
            public byte C;
            public uint D;
        }

        [Fact]
        public void StructCanBeSerialized()
        {
            var testStruct = default(TestStruct);
            testStruct.A = 13337;
            testStruct.B = 1337f;
            testStruct.C = 137;
            testStruct.D = 17;

            var buffer = new byte[13];
            var writeSpan = new Span<byte>(buffer);
            BinSerialize.WriteStruct(ref writeSpan, testStruct);

            var readSpan = new ReadOnlySpan<byte>(buffer);
            var readStruct = BinSerialize.ReadStruct<TestStruct>(ref readSpan);

            Assert.Equal(testStruct.A, readStruct.A);
            Assert.Equal(testStruct.B, readStruct.B);
            Assert.Equal(testStruct.C, readStruct.C);
            Assert.Equal(testStruct.D, readStruct.D);
        }

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

        [Fact]
        public void BoolCanBeReserved()
        {
            var buffer = new byte[1];
            var writeSpan = new Span<byte>(buffer);

            ref bool reserved = ref BinSerialize.ReserveBool(ref writeSpan);
            reserved = true;

            var readSpan = new ReadOnlySpan<byte>(buffer);
            Assert.True(BinSerialize.ReadBool(ref readSpan));
        }

        [Fact]
        public void ByteCanBeReserved()
        {
            var buffer = new byte[1];
            var writeSpan = new Span<byte>(buffer);

            ref byte reserved = ref BinSerialize.ReserveByte(ref writeSpan);
            reserved = 137;

            var readSpan = new ReadOnlySpan<byte>(buffer);
            Assert.Equal(137, BinSerialize.ReadByte(ref readSpan));
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

        [Fact]
        public void UShortCanBeReserved()
        {
            var buffer = new byte[2];
            var writeSpan = new Span<byte>(buffer);

            ref ushort reserved = ref BinSerialize.ReserveUShort(ref writeSpan);
            reserved = 1337;

            var readSpan = new ReadOnlySpan<byte>(buffer);
            Assert.Equal(1337, BinSerialize.ReadUShort(ref readSpan));
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

        [Fact]
        public void UIntCanBeReserved()
        {
            var buffer = new byte[4];
            var writeSpan = new Span<byte>(buffer);

            ref uint reserved = ref BinSerialize.ReserveUInt(ref writeSpan);
            reserved = 133337;

            var readSpan = new ReadOnlySpan<byte>(buffer);
            Assert.Equal((uint)133337, BinSerialize.ReadUInt(ref readSpan));
        }

        [Fact]
        public void LongCanBeReserved()
        {
            var buffer = new byte[8];
            var writeSpan = new Span<byte>(buffer);

            ref long reserved = ref BinSerialize.ReserveLong(ref writeSpan);
            reserved = -1333333337;

            var readSpan = new ReadOnlySpan<byte>(buffer);
            Assert.Equal(-1333333337, BinSerialize.ReadLong(ref readSpan));
        }

        [Fact]
        public void ULongCanBeReserved()
        {
            var buffer = new byte[8];
            var writeSpan = new Span<byte>(buffer);

            ref ulong reserved = ref BinSerialize.ReserveULong(ref writeSpan);
            reserved = 1333333337;

            var readSpan = new ReadOnlySpan<byte>(buffer);
            Assert.Equal(1333333337, BinSerialize.ReadLong(ref readSpan));
        }

        [Fact]
        public void StructCanBeReserved()
        {
            var buffer = new byte[13];
            var writeSpan = new Span<byte>(buffer);

            ref TestStruct reserved = ref BinSerialize.ReserveStruct<TestStruct>(ref writeSpan);
            reserved.A = 13337;
            reserved.B = 1337f;
            reserved.C = 137;
            reserved.D = 17;

            var readSpan = new ReadOnlySpan<byte>(buffer);
            var readStruct = BinSerialize.ReadStruct<TestStruct>(ref readSpan);

            Assert.Equal(reserved.A, readStruct.A);
            Assert.Equal(reserved.B, readStruct.B);
            Assert.Equal(reserved.C, readStruct.C);
            Assert.Equal(reserved.D, readStruct.D);
        }

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
        [InlineData(uint.MinValue)]
        [InlineData(uint.MaxValue)]
        [InlineData((uint)int.MaxValue + 1337)]
        public void PackedUnsignedIntegerWriteCanBeEstimated(uint val)
        {
            var expectedBytes = BinSerialize.GetSizeForPackedUnsignedInteger(val);

            var buffer = new byte[64];
            var writeSpan = new Span<byte>(buffer);
            BinSerialize.WritePackedUnsignedInteger(ref writeSpan, val);
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
        [InlineData(0, 1)]
        [InlineData(127, 1)]
        [InlineData(128, 2)]
        [InlineData(16383, 2)]
        [InlineData(16384, 3)]
        [InlineData(2097151, 3)]
        [InlineData(2097152, 4)]
        [InlineData(268435455, 4)]
        [InlineData(268435456, 5)]
        public void PackedUnsignedIntegerWriteIsExpectedSize(uint val, int expectedSize) =>
            Assert.Equal(expectedSize, BinSerialize.GetSizeForPackedUnsignedInteger(val));

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
                    binaryWriter.Write(val);
                memStreamBytesWritten = (int)memStream.Length;
            }

            var buffer = new byte[128];
            var writeSpan = new Span<byte>(buffer);
            BinSerialize.WriteString(ref writeSpan, val);
            var writtenBytes = buffer.Length - writeSpan.Length;

            Assert.Equal(memStreamBytesWritten, writtenBytes);
            Assert.True(
                memStreamBuffer.AsSpan().Slice(memStreamBytesWritten).SequenceEqual(buffer.AsSpan().Slice(writtenBytes)));
        }
    }
}
