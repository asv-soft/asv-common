// ChimpEncoderTests.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Asv.Common;
using Asv.IO;
using Xunit;

namespace Asv.IO.Tests
{
    /// <summary>Minimal MSB-first bit writer for testing.</summary>
    internal sealed class TestBitWriter : IBitWriter
    {
        private readonly List<byte> _bits = new(); // stores 0/1 per bit
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
            if (count is <= 0 or > 64)
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

        // Helpers for assertions
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

        public ValueTask DisposeAsync()
        {
            _disposed = true;
            return ValueTask.CompletedTask;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TestBitWriter));
            }
        }
    }

    public class ChimpEncoderTests
    {
        private static void AddBits(List<byte> dst, ulong value, int width)
        {
            // MSB-first append
            for (var i = width - 1; i >= 0; i--)
            {
                dst.Add((byte)((value >> i) & 1UL));
            }
        }

        private static string BitsConcat(params (ulong v, int w)[] chunks)
        {
            var bits = new List<byte>();
            foreach (var (v, w) in chunks)
                AddBits(bits, v, w);
            var chars = new char[bits.Count];
            for (var i = 0; i < bits.Count; i++)
            {
                chars[i] = bits[i] == 0 ? '0' : '1';
            }

            return new string(chars);
        }

        [Fact]
        public void FirstSample_Writes64BitsVerbatim()
        {
            var writer = new TestBitWriter();
            using var enc = new ChimpEncoder(writer, leaveOpen: true);

            var first = 0x0123_4567_89AB_CDEFUL;
            enc.Add(first);

            // Expect exactly 64 bits equal to 'first'
            Assert.Equal(64, writer.TotalBitsWritten);

            var expected = BitsConcat((first, 64));
            Assert.Equal(expected, writer.ToBitString());
        }

        [Fact]
        public void Repeat_Prefix0_WhenXorIsZero()
        {
            var writer = new TestBitWriter();
            using var enc = new ChimpEncoder(writer, leaveOpen: true);

            var v = 0xAAAA_AAAA_AAAA_AAAAUL;
            enc.Add(v); // first: 64 bits
            enc.Add(v); // repeat: prefix '0'

            var expected = BitsConcat((v, 64), (0b0UL, 1));
            Assert.Equal(expected, writer.ToBitString());
        }

        [Fact]
        public void DefineWindow_Writes_11_L_Wminus1_Payload()
        {
            var writer = new TestBitWriter();
            using var enc = new ChimpEncoder(writer, leaveOpen: true);

            var prev = 0x0123_4567_89AB_CDEFUL;

            // Choose XOR window: L=1, W=3, payload=0b101 → xor = payload << (64-1-3) = << 60.
            int L = 1,
                W = 3;
            var payload = 0b101UL;
            var xor = payload << (64 - L - W);
            var cur = prev ^ xor;

            enc.Add(prev); // first
            enc.Add(cur); // must define window

            var expected = BitsConcat(
                (prev, 64),
                (0b11UL, 2),
                ((ulong)L, 5),
                ((ulong)(W - 1), 6),
                (payload, W)
            );
            Assert.Equal(expected, writer.ToBitString());
        }

        [Fact]
        public void ReuseWindow_Writes_10_And_PayloadOnly()
        {
            var writer = new TestBitWriter();
            using var enc = new ChimpEncoder(writer, leaveOpen: true);

            var prev = 0x0123_4567_89AB_CDEFUL;

            // Define L=2, W=4, payload1=0b1111  → sig = 4
            int L = 2,
                W = 4;
            ulong payload1 = 0b1111UL;
            ulong xor1 = payload1 << (64 - L - W);
            ulong cur1 = prev ^ xor1;

            // Reuse in same [L,W] with payload2=0b0011
            ulong payload2 = 0b0011UL;
            ulong xor2 = payload2 << (64 - L - W);
            ulong cur2 = cur1 ^ xor2;

            enc.Add(prev); // first
            enc.Add(cur1); // define window
            enc.Add(cur2); // reuse window

            var expected = BitsConcat(
                (prev, 64),
                (0b11UL, 2),
                ((ulong)L, 5),
                ((ulong)(W - 1), 6),
                (payload1, W),
                (0b10UL, 2),
                (payload2, W)
            );
            Assert.Equal(expected, writer.ToBitString());
        }

        [Fact]
        public void WEquals64_Path_NoShiftNoMask()
        {
            var writer = new TestBitWriter();
            using var enc = new ChimpEncoder(writer, leaveOpen: true);

            var prev = 0UL;
            int L = 0,
                W = 64;
            var payload = 0xF0F0_F0F0_F0F0_F0F1UL;
            var cur = prev ^ payload; // xor == payload

            enc.Add(prev); // first
            enc.Add(cur); // define with W=64

            var expected = BitsConcat(
                (prev, 64),
                (0b11UL, 2),
                (0UL, 5), // L=0
                (63UL, 6), // (W-1)=63
                (payload, 64) // full-lane payload
            );
            Assert.Equal(expected, writer.ToBitString());
        }

        [Fact]
        public void NonReusable_NewWindow_When_Xor_DoesNotFit_StickyWindow()
        {
            var writer = new TestBitWriter();
            using var enc = new ChimpEncoder(writer, leaveOpen: true);

            var prev = 0xFFFF_0000_0000_0000UL;

            // First window: L1=0, W1=2, payload1=0b11
            int L1 = 0,
                W1 = 2;
            var payload1 = 0b11UL;
            var xor1 = payload1 << (64 - L1 - W1);
            var cur1 = prev ^ xor1;

            // Second XOR: pattern 1010 in top nibble → фактически sig=3, W2=3, payload=0b101
            int L2 = 0,
                W2 = 3;
            var payload2_effective = 0b101UL;
            var xor2 = payload2_effective << (64 - L2 - W2); // то есть биты 63 и 61 установлены
            var cur2 = cur1 ^ xor2;

            enc.Add(prev); // first
            enc.Add(cur1); // define [L1,W1]
            enc.Add(cur2); // NOT reusable (3 > 2) → define new window

            var expected = BitsConcat(
                (prev, 64),
                (0b11UL, 2),
                ((ulong)L1, 5),
                ((ulong)(W1 - 1), 6),
                (payload1, W1),
                (0b11UL, 2),
                ((ulong)L2, 5),
                ((ulong)(W2 - 1), 6),
                (payload2_effective, W2)
            );

            Assert.Equal(expected, writer.ToBitString());
        }

        [Fact]
        public void Dispose_LeaveOpen_False_DisposesWriter()
        {
            var writer = new TestBitWriter();
            var enc = new ChimpEncoder(writer, leaveOpen: false);

            enc.Add(0UL);
            enc.Dispose();

            Assert.True(writer.IsDisposed);
        }

        [Fact]
        public async Task DisposeAsync_LeaveOpen_True_DoesNotDisposeWriter()
        {
            var writer = new TestBitWriter();
            var enc = new ChimpEncoder(writer, leaveOpen: true);

            enc.Add(0UL);
            await enc.DisposeAsync();

            Assert.False(writer.IsDisposed);
        }
    }
}
