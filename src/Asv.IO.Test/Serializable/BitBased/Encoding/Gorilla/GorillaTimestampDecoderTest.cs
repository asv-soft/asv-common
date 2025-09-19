// GorillaTimestampDecoderTests.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Asv.Common;
using Asv.IO;
using Xunit;

namespace Asv.IO.Tests
{
    public class GorillaTimestampDecoderTests
    {
        // -------- Helpers --------
        private sealed class BitStreamBuilder
        {
            private readonly List<byte> _bits = new(); // 0/1 per bit (MSB-first)

            public long LengthBits => _bits.Count;

            public IReadOnlyList<byte> ToBitArray() => _bits;

            public void AddBit(int bit)
            {
                if ((uint)bit > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(bit));
                }

                _bits.Add((byte)bit);
            }

            public void AddBits(ulong value, int width)
            {
                if (width <= 0 || width > 64)
                {
                    throw new ArgumentOutOfRangeException(nameof(width));
                }

                for (int i = width - 1; i >= 0; i--)
                {
                    var b = (value >> i) & 1UL;
                    _bits.Add((byte)b);
                }
            }

            public void AddSigned(long value, int width)
            {
                if (width <= 0 || width > 64)
                {
                    throw new ArgumentOutOfRangeException(nameof(width));
                }

                ulong u = (ulong)value & (width == 64 ? ulong.MaxValue : ((1UL << width) - 1));
                AddBits(u, width);
            }
        }

        /// <summary>Simple IBitReader over a prepared bit array (MSB-first).</summary>
        private sealed class TestBitReader : IBitReader
        {
            private readonly IReadOnlyList<byte> _bits;
            private int _pos;
            private bool _disposed;

            public TestBitReader(IReadOnlyList<byte> bits) => _bits = bits;

            public long TotalBitsRead { get; private set; }
            public bool IsDisposed => _disposed;

            public int ReadBit()
            {
                EnsureNotDisposed();
                if (_pos >= _bits.Count)
                {
                    throw new EndOfStreamException("No more bits.");
                }

                int bit = _bits[_pos++];
                TotalBitsRead++;
                return bit;
            }

            public ulong ReadBits(int count)
            {
                if (count <= 0 || count > 64)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                EnsureNotDisposed();
                ulong v = 0;
                for (int i = 0; i < count; i++)
                {
                    if (_pos >= _bits.Count)
                    {
                        throw new EndOfStreamException("No more bits.");
                    }

                    v = (v << 1) | _bits[_pos++];
                }
                TotalBitsRead += count;
                return v;
            }

            public void AlignToByte()
            {
                throw new NotImplementedException();
            }

            public void Dispose() => _disposed = true;

            public ValueTask DisposeAsync()
            {
                _disposed = true;
                return ValueTask.CompletedTask;
            }

            private void EnsureNotDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(TestBitReader));
                }
            }
        }

        private static long ApplyDod(long prevDelta, long dod) => prevDelta + dod;

        // -------- Tests --------
        [Fact]
        public void Reads_t0_Verbatim_AndCountsBits()
        {
            var b = new BitStreamBuilder();
            long t0 = unchecked((long)0x0123_4567_89AB_CDEFUL);
            b.AddBits((ulong)t0, 64);

            var rdr = new TestBitReader(b.ToBitArray());
            using var dec = new GorillaTimestampDecoder(
                rdr,
                firstDelta27Bits: true,
                leaveOpen: true
            );

            var v0 = dec.ReadNext();
            Assert.Equal(t0, v0);
            Assert.Equal(64, dec.TotalBitsRead);
        }

        [Theory]
        [InlineData(5)] // positive Δ1
        [InlineData(-3)] // negative Δ1
        public void Delta1_27bit_SignExtended(long delta1)
        {
            var b = new BitStreamBuilder();
            long t0 = 1_000_000;
            b.AddBits((ulong)t0, 64);
            b.AddSigned(delta1, 27); // Δ1 (27-bit two’s complement)

            var rdr = new TestBitReader(b.ToBitArray());
            using var dec = new GorillaTimestampDecoder(
                rdr,
                firstDelta27Bits: true,
                leaveOpen: true
            );

            var t0r = dec.ReadNext();
            var t1r = dec.ReadNext();
            Assert.Equal(t0, t0r);
            Assert.Equal(t0 + delta1, t1r);
        }

        [Fact]
        public void Delta1_64bit_WhenFirstDelta27BitsFalse()
        {
            var b = new BitStreamBuilder();
            long t0 = 100;
            ulong delta1 = 123UL;
            b.AddBits((ulong)t0, 64);
            b.AddBits(delta1, 64); // raw 64-bit Δ1

            var rdr = new TestBitReader(b.ToBitArray());
            using var dec = new GorillaTimestampDecoder(
                rdr,
                firstDelta27Bits: false,
                leaveOpen: true
            );

            var t0r = dec.ReadNext();
            var t1r = dec.ReadNext();
            Assert.Equal(t0, t0r);
            Assert.Equal(t0 + (long)delta1, t1r);
        }

        [Fact]
        public void DoD_Prefix0_RepeatsPreviousDelta()
        {
            var b = new BitStreamBuilder();
            long t0 = 1000;
            long delta1 = 10;

            // t0 + Δ1(27) + '0'
            b.AddBits((ulong)t0, 64);
            b.AddSigned(delta1, 27);
            b.AddBit(0); // DoD=0

            var rdr = new TestBitReader(b.ToBitArray());
            using var dec = new GorillaTimestampDecoder(rdr, true, leaveOpen: true);

            var t0r = dec.ReadNext();
            var t1r = dec.ReadNext(); // t0 + 10
            var t2r = dec.ReadNext(); // t1 + prevDelta(10) => t0 + 20

            Assert.Equal(t0, t0r);
            Assert.Equal(t0 + 10, t1r);
            Assert.Equal(t0 + 20, t2r);
        }

        [Theory]
        [InlineData(-64)] // min 7-bit
        [InlineData(63)] // max 7-bit
        public void DoD_Prefix10_7bit_Signed(long dod)
        {
            var b = new BitStreamBuilder();
            long t0 = 0;
            long delta1 = 5;

            b.AddBits((ulong)t0, 64);
            b.AddSigned(delta1, 27); // Δ1
            b.AddBits(0b10, 2); // prefix
            b.AddSigned(dod, 7); // 7-bit DoD

            var rdr = new TestBitReader(b.ToBitArray());
            using var dec = new GorillaTimestampDecoder(rdr, true, leaveOpen: true);

            var t0r = dec.ReadNext();
            var t1r = dec.ReadNext(); // 5
            var delta2 = ApplyDod(delta1, dod);
            var t2r = dec.ReadNext(); // 5 + (5 + dod) = 10 + dod

            Assert.Equal(t0, t0r);
            Assert.Equal(t0 + delta1, t1r);
            Assert.Equal(t1r + delta2, t2r);
        }

        [Theory]
        [InlineData(-256)] // min 9-bit
        [InlineData(255)] // max 9-bit
        [InlineData(200)]
        public void DoD_Prefix110_9bit_Signed(long dod)
        {
            var b = new BitStreamBuilder();
            long t0 = 10;
            long delta1 = -7;

            b.AddBits((ulong)t0, 64);
            b.AddSigned(delta1, 27);
            b.AddBits(0b110, 3);
            b.AddSigned(dod, 9);

            var rdr = new TestBitReader(b.ToBitArray());
            using var dec = new GorillaTimestampDecoder(rdr, true, leaveOpen: true);

            var t0r = dec.ReadNext();
            var t1r = dec.ReadNext(); // 10 - 7 = 3
            var delta2 = ApplyDod(delta1, dod);
            var t2r = dec.ReadNext();

            Assert.Equal(t0, t0r);
            Assert.Equal(t0 + delta1, t1r);
            Assert.Equal(t1r + delta2, t2r);
        }

        [Theory]
        [InlineData(-2048)] // min 12-bit
        [InlineData(2047)] // max 12-bit
        [InlineData(-1000)]
        public void DoD_Prefix1110_12bit_Signed(long dod)
        {
            var b = new BitStreamBuilder();
            long t0 = -1000;
            long delta1 = 1;

            b.AddBits((ulong)t0, 64);
            b.AddSigned(delta1, 27);
            b.AddBits(0b1110, 4);
            b.AddSigned(dod, 12);

            var rdr = new TestBitReader(b.ToBitArray());
            using var dec = new GorillaTimestampDecoder(rdr, true, leaveOpen: true);

            var t0r = dec.ReadNext();
            var t1r = dec.ReadNext(); // -999
            var delta2 = ApplyDod(delta1, dod);
            var t2r = dec.ReadNext();

            Assert.Equal(t0, t0r);
            Assert.Equal(t0 + delta1, t1r);
            Assert.Equal(t1r + delta2, t2r);
        }

        [Theory]
        [InlineData(0UL)]
        [InlineData(1UL)]
        [InlineData(0x0000_0000UL)]
        [InlineData(0x0000_FF00UL)]
        [InlineData(0x7FFF_FFFFUL)]
        [InlineData(0x8000_0000UL)] // still treated as +2147483648 when cast to long (then added to prevDelta)
        [InlineData(0xFFFF_FFFFUL)]
        public void DoD_Prefix1111_32bit_Unsigned(ulong dod32)
        {
            var b = new BitStreamBuilder();
            long t0 = 0;
            long delta1 = 2;

            b.AddBits((ulong)t0, 64);
            b.AddSigned(delta1, 27);
            b.AddBits(0b1111, 4);
            b.AddBits(dod32, 32); // unsigned 32-bit DoD

            var rdr = new TestBitReader(b.ToBitArray());
            using var dec = new GorillaTimestampDecoder(rdr, true, leaveOpen: true);

            var t0r = dec.ReadNext();
            var t1r = dec.ReadNext();
            var dodLong = (long)dod32; // decoder casts 32-bit unsigned to long as-is
            var delta2 = ApplyDod(delta1, dodLong); // Δ2 = Δ1 + DoD
            var t2r = dec.ReadNext();

            Assert.Equal(t0, t0r);
            Assert.Equal(delta1, t1r - t0r);
            Assert.Equal(delta2, t2r - t1r);
        }

        [Fact]
        public void TotalBitsRead_IsAccurate_ForMixedSequence()
        {
            var b = new BitStreamBuilder();
            long t0 = 100;
            long delta1 = 7; // 27 bits
            long dod7 = -1; // prefix '10' + 7 bits
            long dod9 = 200; // prefix '110' + 9 bits
            long dod12 = -1000; // prefix '1110' + 12 bits
            ulong dod32 = 0x00000010; // prefix '1111' + 32 bits

            // Build stream
            b.AddBits((ulong)t0, 64);
            b.AddSigned(delta1, 27);
            b.AddBits(0b10, 2);
            b.AddSigned(dod7, 7);
            b.AddBits(0b110, 3);
            b.AddSigned(dod9, 9);
            b.AddBits(0b1110, 4);
            b.AddSigned(dod12, 12);
            b.AddBits(0b1111, 4);
            b.AddBits(dod32, 32);

            long expected =
                64
                + // t0
                27
                + // Δ1
                (2 + 7)
                + // '10' + 7
                (3 + 9)
                + // '110' + 9
                (4 + 12)
                + // '1110' + 12
                (4 + 32); // '1111' + 32

            var rdr = new TestBitReader(b.ToBitArray());
            using var dec = new GorillaTimestampDecoder(
                rdr,
                firstDelta27Bits: true,
                leaveOpen: true
            );

            // Consume all values (t0, t1, t2, t3, t4, t5)
            _ = dec.ReadNext(); // t0
            _ = dec.ReadNext(); // t1
            _ = dec.ReadNext(); // after '10'
            _ = dec.ReadNext(); // after '110'
            _ = dec.ReadNext(); // after '1110'
            _ = dec.ReadNext(); // after '1111'

            Assert.Equal(expected, dec.TotalBitsRead);
        }

        [Fact]
        public void Dispose_LeaveOpenFalse_DisposesReader()
        {
            var b = new BitStreamBuilder();
            b.AddBits(123UL, 64);

            var rdr = new TestBitReader(b.ToBitArray());
            var dec = new GorillaTimestampDecoder(rdr, leaveOpen: false);

            _ = dec.ReadNext();
            dec.Dispose();

            Assert.True(rdr.IsDisposed);
        }

        [Fact]
        public async Task DisposeAsync_LeaveOpenTrue_DoesNotDisposeReader()
        {
            var b = new BitStreamBuilder();
            b.AddBits(123UL, 64);

            var rdr = new TestBitReader(b.ToBitArray());
            var dec = new GorillaTimestampDecoder(rdr, leaveOpen: true);

            _ = dec.ReadNext();
            await dec.DisposeAsync();

            Assert.False(rdr.IsDisposed);
        }
    }
}
