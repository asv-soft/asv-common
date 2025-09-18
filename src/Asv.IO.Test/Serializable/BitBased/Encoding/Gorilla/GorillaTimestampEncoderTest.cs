// GorillaTimestampEncoderTests.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Asv.Common;
using Asv.IO;
using Xunit;

namespace Asv.IO.Tests
{
    

    public class GorillaTimestampEncoderTests
    {
        /// <summary>Minimal MSB-first bit writer for testing.</summary>
        internal sealed class TestBitWriter : IBitWriter
        {
            private readonly List<byte> _bits = new();
            private bool _disposed;

            public long TotalBitsWritten { get; private set; }
            public bool IsDisposed => _disposed;

            public void WriteBit(int bit)
            {
                EnsureNotDisposed();
                if ((uint)bit > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(bit));
                }

                _bits.Add((byte)bit);
                TotalBitsWritten++;
            }

            public void WriteBits(ulong value, int count)
            {
                EnsureNotDisposed();
                if (count <= 0 || count > 64)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                for (var i = count - 1; i >= 0; i--)
                {
                    var b = (value >> i) & 1UL;
                    _bits.Add((byte)b);
                }
                TotalBitsWritten += count;
            }

            public void AlignToByte()
            {
                throw new NotImplementedException();
            }

            public void Flush()
            {
                throw new NotImplementedException();
            }

            public IReadOnlyList<byte> Bits => _bits;

            public string ToBitString()
            {
                var chars = new char[_bits.Count];
                for (var i = 0; i < _bits.Count; i++)
                {
                    chars[i] = _bits[i] == 0 ? '0' : '1';
                }

                return new string(chars);
            }

            public void Dispose() => _disposed = true;
            public ValueTask DisposeAsync() { _disposed = true; return ValueTask.CompletedTask; }

            private void EnsureNotDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(TestBitWriter));
                }
            }
        }
        
        // ---------- Helpers for expected bitstreams ----------
        private static void AddBits(List<byte> dst, ulong value, int width)
        {
            for (var i = width - 1; i >= 0; i--)
            {
                dst.Add((byte)((value >> i) & 1UL));
            }
        }

        private static void AddSigned(List<byte> dst, long value, int width)
        {
            var u = (ulong)value & (width == 64 ? ulong.MaxValue : ((1UL << width) - 1));
            AddBits(dst, u, width);
        }

        private static string BitsConcat(params (ulong v, int w)[] chunks)
        {
            var bits = new List<byte>();
            foreach (var (v, w) in chunks) AddBits(bits, v, w);
            var chars = new char[bits.Count];
            for (var i = 0; i < bits.Count; i++)
            {
                chars[i] = bits[i] == 0 ? '0' : '1';
            }

            return new string(chars);
        }

        // ---------- Tests ----------
        [Fact]
        public void T0_Written_As_64_Bits()
        {
            var w = new TestBitWriter();
            using var enc = new GorillaTimestampEncoder(w);

            const long t0 = unchecked((long)0x0123_4567_89AB_CDEFUL);
            enc.Add(t0);

            var expected = BitsConcat(((ulong)t0, 64));
            Assert.Equal(expected, w.ToBitString());
            Assert.Equal(64, w.TotalBitsWritten);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(-3)]
        [InlineData(0)]
        public void Delta1_As_27bit_TwosComplement(long delta1)
        {
            var w = new TestBitWriter();
            using var enc = new GorillaTimestampEncoder(w, firstDelta27Bits: true);

            const long t0 = 1000;
            var t1 = t0 + delta1;

            enc.Add(t0);
            enc.Add(t1);

            var bits = new List<byte>();
            AddBits(bits, (ulong)t0, 64);
            AddSigned(bits, delta1, 27);

            Assert.Equal(ToString(bits), w.ToBitString());
        }

        [Fact]
        public void Delta1_As_64bit_When_Option_False()
        {
            var w = new TestBitWriter();
            using var enc = new GorillaTimestampEncoder(w, firstDelta27Bits: false);

            const long t0 = 100;
            const long delta1 = 0x0000_0001_FFFF_FFF0; // any 64-bit pattern (fits positive)
            var t1 = t0 + delta1;

            enc.Add(t0);
            enc.Add(t1);

            var expected = BitsConcat(
                ((ulong)t0, 64),
                ((ulong)delta1, 64)
            );
            Assert.Equal(expected, w.ToBitString());
        }

        [Fact]
        public void DoD_Prefix0_When_Delta_Untouched()
        {
            var w = new TestBitWriter();
            using var enc = new GorillaTimestampEncoder(w);

            const long t0 = 0;
            const long delta1 = 10;
            var t1 = t0 + delta1;
            var t2 = t1 + delta1; // DoD = 0

            enc.Add(t0);
            enc.Add(t1);
            enc.Add(t2);

            var expected = new List<byte>();
            AddBits(expected, (ulong)t0, 64);
            AddSigned(expected, delta1, 27);
            AddBits(expected, 0b0, 1); // DoD=0

            Assert.Equal(ToString(expected), w.ToBitString());
        }

        [Theory]
        [InlineData(-64)]
        [InlineData(63)]
        [InlineData(7)]
        public void DoD_Prefix10_7bit_Range(long dod)
        {
            var w = new TestBitWriter();
            using var enc = new GorillaTimestampEncoder(w);

            const long t0 = 0;
            const long delta1 = 5;
            var delta2 = delta1 + dod;
            var t1 = t0 + delta1;
            var t2 = t1 + delta2;

            enc.Add(t0);
            enc.Add(t1);
            enc.Add(t2);

            var exp = new List<byte>();
            AddBits(exp, (ulong)t0, 64);
            AddSigned(exp, delta1, 27);
            AddBits(exp, 0b10, 2);
            AddSigned(exp, dod, 7);

            Assert.Equal(ToString(exp), w.ToBitString());
        }

        [Theory]
        [InlineData(-256)]
        [InlineData(255)]
        [InlineData(120)]
        public void DoD_Prefix110_9bit_Range(long dod)
        {
            var w = new TestBitWriter();
            using var enc = new GorillaTimestampEncoder(w);

            const long t0 = 10;
            const long delta1 = -7;
            var delta2 = delta1 + dod;
            var t1 = t0 + delta1;
            var t2 = t1 + delta2;

            enc.Add(t0);
            enc.Add(t1);
            enc.Add(t2);

            var exp = new List<byte>();
            AddBits(exp, (ulong)t0, 64);
            AddSigned(exp, delta1, 27);
            AddBits(exp, 0b110, 3);
            AddSigned(exp, dod, 9);

            Assert.Equal(ToString(exp), w.ToBitString());
        }

        [Theory]
        [InlineData(-2048)]
        [InlineData(2047)]
        [InlineData(-1000)]
        public void DoD_Prefix1110_12bit_Range(long dod)
        {
            var w = new TestBitWriter();
            using var enc = new GorillaTimestampEncoder(w);

            const long t0 = -1000;
            const long delta1 = 1;
            var delta2 = delta1 + dod;
            var t1 = t0 + delta1;
            var t2 = t1 + delta2;

            enc.Add(t0);
            enc.Add(t1);
            enc.Add(t2);

            var exp = new List<byte>();
            AddBits(exp, unchecked((ulong)t0), 64);
            AddSigned(exp, delta1, 27);
            AddBits(exp, 0b1110, 4);
            AddSigned(exp, dod, 12);

            Assert.Equal(ToString(exp), w.ToBitString());
        }

        [Theory]
        [InlineData(0x0000_0800UL)]   // 2048 — сразу за пределом 12-bit (max 2047)
        [InlineData(0x0000_FF00UL)]
        [InlineData(0x7FFF_FFFFUL)]
        [InlineData(0x8000_0000UL)]
        [InlineData(0xFFFF_FFFFUL)]
        public void DoD_Prefix1111_32bit_Raw(ulong dod32)
        {
            var w = new TestBitWriter();
            using var enc = new GorillaTimestampEncoder(w);

            const long t0 = 0;
            const long delta1 = 0;
            var t1 = t0 + delta1;

            var delta2 = (long)dod32;   // DoD = delta2 - delta1 = dod32
            var t2 = t1 + delta2;

            enc.Add(t0);
            enc.Add(t1);
            enc.Add(t2);

            var exp = new List<byte>();
            AddBits(exp, (ulong)t0, 64);
            AddSigned(exp, delta1, 27);
            AddBits(exp, 0b1111, 4);
            AddBits(exp, dod32, 32);

            Assert.Equal(ToString(exp), w.ToBitString());
        }


        [Fact]
        public void TotalBitsWritten_IsAccurate_MixedSequence()
        {
            var w = new TestBitWriter();
            using var enc = new GorillaTimestampEncoder(w);

            const long t0 = 100;
            const long delta1 = 7;           // 27 bits
            const long dod7   = -1;          // '10'  + 7
            const long dod9   = 200;         // '110' + 9
            const long dod12  = -1000;       // '1110' + 12
            const ulong dod32 = 0x1_0000UL;  // '1111' + 32 (65536 -> за пределами 12-бит)

            var t1 = t0 + delta1;
            var t2 = t1 + (delta1 + dod7);
            var t3 = t2 + (delta1 + dod7 + dod9);
            var t4 = t3 + (delta1 + dod7 + dod9 + dod12);
            var t5 = t4 + (delta1 + dod7 + dod9 + dod12 + (long)dod32);

            enc.Add(t0); // 64
            enc.Add(t1); // 27
            enc.Add(t2); // 2 + 7
            enc.Add(t3); // 3 + 9
            enc.Add(t4); // 4 + 12
            enc.Add(t5); // 4 + 32

            const long expectedBits =
                64 +        // t0
                27 +        // Δ1
                (2 + 7) +   // '10'  + 7
                (3 + 9) +   // '110' + 9
                (4 + 12) +  // '1110'+ 12
                (4 + 32);   // '1111'+ 32

            Assert.Equal(expectedBits, w.TotalBitsWritten);
        }


        [Fact]
        public void Dispose_LeaveOpenFalse_DisposesWriter()
        {
            var w = new TestBitWriter();
            var enc = new GorillaTimestampEncoder(w, leaveOpen: false);

            enc.Add(0);
            enc.Dispose();

            Assert.True(w.IsDisposed);
        }

        [Fact]
        public async Task DisposeAsync_LeaveOpenTrue_DoesNotDisposeWriter()
        {
            var w = new TestBitWriter();
            var enc = new GorillaTimestampEncoder(w, leaveOpen: true);

            enc.Add(0);
            await enc.DisposeAsync();

            Assert.False(w.IsDisposed);
        }

        [Fact]
        public void RoundTrip_Encoder_To_Decoder_Sanity()
        {
            var w = new TestBitWriter();
            using var enc = new GorillaTimestampEncoder(w);

            // Build a sequence that hits several buckets
            const long t0 = 1_000_000;
            const long d1 = -12;                      // 27-bit
            var d2 = d1 + 0;                   // DoD=0
            var d3 = d2 + 63;                  // 7-bit max
            var d4 = d3 - 256;                 // 9-bit min
            var d5 = d4 + 2047;                // 12-bit max
            var d6 = d5 + (long)0x1_0000;      // 32-bit bucket

            var t1 = t0 + d1;
            var t2 = t1 + d2;
            var t3 = t2 + d3;
            var t4 = t3 + d4;
            var t5 = t4 + d5;
            var t6 = t5 + d6;

            enc.Add(t0);
            enc.Add(t1);
            enc.Add(t2);
            enc.Add(t3);
            enc.Add(t4);
            enc.Add(t5);
            enc.Add(t6);

            // Decode back
            var reader = new TestBitReader(w.Bits);
            using var dec = new GorillaTimestampDecoder(reader, firstDelta27Bits: true, leaveOpen: true);

            Assert.Equal(t0, dec.ReadNext());
            Assert.Equal(t1, dec.ReadNext());
            Assert.Equal(t2, dec.ReadNext());
            Assert.Equal(t3, dec.ReadNext());
            Assert.Equal(t4, dec.ReadNext());
            Assert.Equal(t5, dec.ReadNext());
            Assert.Equal(t6, dec.ReadNext());
        }

        // Reader for the round-trip test (MSB-first over captured bits)
        private sealed class TestBitReader : IBitReader
        {
            private readonly IReadOnlyList<byte> _bits;
            private int _pos;
            public TestBitReader(IReadOnlyList<byte> bits) => _bits = bits;

            public long TotalBitsRead { get; private set; }

            public int ReadBit()
            {
                if (_pos >= _bits.Count)
                {
                    throw new System.IO.EndOfStreamException();
                }

                TotalBitsRead++;
                return _bits[_pos++];
            }

            public ulong ReadBits(int count)
            {
                if (count <= 0 || count > 64)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                ulong v = 0;
                for (var i = 0; i < count; i++)
                {
                    if (_pos >= _bits.Count)
                    {
                        throw new System.IO.EndOfStreamException();
                    }

                    v = (v << 1) | (ulong)_bits[_pos++];
                }
                TotalBitsRead += count;
                return v;
            }

            public void AlignToByte()
            {
                throw new NotImplementedException();
            }

            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }

        private static string ToString(List<byte> bits)
        {
            var chars = new char[bits.Count];
            for (var i = 0; i < bits.Count; i++)
            {
                chars[i] = bits[i] == 0 ? '0' : '1';
            }

            return new string(chars);
        }
    }
}
