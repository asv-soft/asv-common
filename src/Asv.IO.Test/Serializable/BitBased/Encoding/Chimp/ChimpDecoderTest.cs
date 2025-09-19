// ChimpDecoderTests.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Asv.Common;
using Asv.IO;
using Xunit;

namespace Asv.IO.Tests
{
    public class ChimpDecoderTests
    {
        // ----------------- Helpers -----------------

        /// <summary>
        /// Упрощённый конструктор битового потока (MSB-first).
        /// </summary>
        private sealed class BitStreamBuilder
        {
            private readonly List<byte> _bits = new(); // хранит 0/1 как байты

            public long LengthBits => _bits.Count;

            public void AddBit(int bit)
            {
                if ((uint)bit > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(bit));
                }

                _bits.Add((byte)bit);
            }

            /// <summary>
            /// Добавляет ровно width бит значения value, в порядке MSB→LSB.
            /// Важно: width может быть от 1 до 64; берутся младшие width бит value.
            /// </summary>
            public void AddBits(ulong value, int width)
            {
                if (width <= 0 || width > 64)
                {
                    throw new ArgumentOutOfRangeException(nameof(width));
                }

                // Пишем от старшего к младшему в окне ширины width.
                for (var i = width - 1; i >= 0; i--)
                {
                    var b = (value >> i) & 1UL;
                    _bits.Add((byte)b);
                }
            }

            public IReadOnlyList<byte> ToBitArray() => _bits;
        }

        /// <summary>
        /// Тестовая реализация IBitReader поверх заранее подготовленного массива бит (MSB-first).
        /// </summary>
        private sealed class TestBitReader : IBitReader
        {
            private readonly IReadOnlyList<byte> _bits;
            private int _pos; // позиция в битах
            private bool _disposed;

            public TestBitReader(IReadOnlyList<byte> bits)
            {
                _bits = bits ?? throw new ArgumentNullException(nameof(bits));
            }

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
                for (var i = 0; i < count; i++)
                {
                    if (_pos >= _bits.Count)
                    {
                        throw new EndOfStreamException("No more bits.");
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

            public void Dispose()
            {
                _disposed = true;
            }

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

        private static ulong MakeValue(ulong prev, int l, int w, ulong payload)
        {
            // xor = payload << (64 - L - W) (кроме случая W==64, но формула всё равно корректна при L=0)
            var tWin = 64 - l - w;
            var xor = (w == 64) ? payload : (payload << tWin);
            return prev ^ xor;
        }

        [Fact]
        public void FirstValue_IsReturnedVerbatim_AndCountsBits()
        {
            var b = new BitStreamBuilder();
            var first = 0x0123_4567_89AB_CDEFUL;
            b.AddBits(first, 64);

            var reader = new TestBitReader(b.ToBitArray());
            using var dec = new ChimpDecoder(reader, leaveOpen: true);

            var v0 = dec.ReadNext();
            Assert.Equal(first, v0);
            Assert.Equal(64, dec.TotalBitsRead);
        }

        [Fact]
        public void RepeatPrevious_p1_0()
        {
            var b = new BitStreamBuilder();
            var first = 0xAAAA_AAAA_AAAA_AAAAUL;
            b.AddBits(first, 64);
            b.AddBit(0); // p1=0 → repeat

            var reader = new TestBitReader(b.ToBitArray());
            using var dec = new ChimpDecoder(reader, leaveOpen: true);

            var v0 = dec.ReadNext();
            var v1 = dec.ReadNext();

            Assert.Equal(first, v0);
            Assert.Equal(first, v1);
        }

        [Fact]
        public void DefineNewWindow_p1_1_p2_1_DecodesCorrectly()
        {
            var b = new BitStreamBuilder();
            var prev = 0x0123_4567_89AB_CDEFUL;

            // Задаём L=1, W=3, payload=0b101 => xor = 0b101 << 60 = 0x5_0000_0000_0000_0000
            int L = 1,
                W = 3;
            var payload = 0b101UL;
            var cur = MakeValue(prev, L, W, payload);

            // Encode:
            b.AddBits(prev, 64); // first
            b.AddBit(1);
            b.AddBit(1); // p1=1, p2=1
            b.AddBits((ulong)L, 5); // L(5)
            b.AddBits((ulong)(W - 1), 6); // (W-1)(6)
            b.AddBits(payload, W); // payload(W)

            var reader = new TestBitReader(b.ToBitArray());
            using var dec = new ChimpDecoder(reader, leaveOpen: true);

            var v0 = dec.ReadNext();
            var v1 = dec.ReadNext();

            Assert.Equal(prev, v0);
            Assert.Equal(cur, v1);
        }

        [Fact]
        public void ReuseWindow_p1_1_p2_0_AfterDefine()
        {
            var b = new BitStreamBuilder();
            var prev = 0x0123_4567_89AB_CDEFUL;

            // Сначала объявим окно L=1, W=3, payload=0b101 → cur1.
            int L = 1,
                W = 3;
            var payload1 = 0b101UL;
            var cur1 = MakeValue(prev, L, W, payload1);

            // Теперь хотим XOR, который помещается в то же окно: payload=0b001 → бит в позиции 60.
            var payload2 = 0b001UL;
            var cur2 = MakeValue(cur1, L, W, payload2);

            // Encode:
            b.AddBits(prev, 64);

            // define new window
            b.AddBit(1);
            b.AddBit(1);
            b.AddBits((ulong)L, 5);
            b.AddBits((ulong)(W - 1), 6);
            b.AddBits(payload1, W);

            // reuse same window
            b.AddBit(1);
            b.AddBit(0);
            b.AddBits(payload2, W);

            var reader = new TestBitReader(b.ToBitArray());
            using var dec = new ChimpDecoder(reader, leaveOpen: true);

            var v0 = dec.ReadNext();
            var v1 = dec.ReadNext();
            var v2 = dec.ReadNext();

            Assert.Equal(prev, v0);
            Assert.Equal(cur1, v1);
            Assert.Equal(cur2, v2);
        }

        [Fact]
        public void DefineWindow_W64_PayloadFillsLane()
        {
            var b = new BitStreamBuilder();
            var prev = 0UL;
            int L = 0,
                W = 64;
            var payload = 0xF0F0_F0F0_F0F0_F0F0UL;
            var cur = prev ^ payload; // т.к. L=0, W=64, xor = payload без сдвига

            // Encode: first + (p1=1,p2=1) + L(5=0) + (W-1)(6=63) + payload(64)
            b.AddBits(prev, 64);
            b.AddBit(1);
            b.AddBit(1);
            b.AddBits(0, 5);
            b.AddBits(63, 6);
            b.AddBits(payload, 64);

            var reader = new TestBitReader(b.ToBitArray());
            using var dec = new ChimpDecoder(reader, leaveOpen: true);

            var v0 = dec.ReadNext();
            var v1 = dec.ReadNext();

            Assert.Equal(prev, v0);
            Assert.Equal(cur, v1);
        }

        [Fact]
        public void ReuseBeforeWindowDefined_Throws()
        {
            var b = new BitStreamBuilder();
            var first = 0xDEAD_BEEF_DEAD_BEEFUL;

            b.AddBits(first, 64); // first value

            // try to reuse window immediately: p1=1, p2=0, но окно ещё не объявлено
            b.AddBit(1);
            b.AddBit(0);

            // (никакого payload, т.к. ширина W неизвестна)
            var reader = new TestBitReader(b.ToBitArray());
            using var dec = new ChimpDecoder(reader, leaveOpen: true);

            _ = dec.ReadNext(); // ok (first)

            var ex = Assert.Throws<InvalidDataException>(() => dec.ReadNext());
            Assert.Contains("Reuse before window defined", ex.Message);
        }

        [Fact]
        public void TotalBitsRead_MatchesEncodedLength()
        {
            var b = new BitStreamBuilder();
            var prev = 0x1111_2222_3333_4444UL;

            // Сценарий: first + define + reuse + repeat
            // define: L=2, W=4, payload=0b1010
            const int l = 2,
                w = 4;
            const ulong payload1 = 0b1010UL;
            var cur1 = MakeValue(prev, l, w, payload1);

            // reuse: payload=0b0011
            var payload2 = 0b0011UL;
            var cur2 = MakeValue(cur1, l, w, payload2);

            b.AddBits(prev, 64); // first
            b.AddBit(1);
            b.AddBit(1); // define
            b.AddBits((ulong)l, 5);
            b.AddBits((ulong)(w - 1), 6);
            b.AddBits(payload1, w);
            b.AddBit(1);
            b.AddBit(0); // reuse
            b.AddBits(payload2, w);
            b.AddBit(0); // repeat previous (cur2)

            long expectedBits =
                64
                + // first
                2
                + 5
                + 6
                + w
                + // define
                2
                + w
                + // reuse
                1; // repeat

            var reader = new TestBitReader(b.ToBitArray());
            using var dec = new ChimpDecoder(reader, leaveOpen: true);

            var v0 = dec.ReadNext();
            var v1 = dec.ReadNext();
            var v2 = dec.ReadNext();
            var v3 = dec.ReadNext();

            Assert.Equal(prev, v0);
            Assert.Equal(cur1, v1);
            Assert.Equal(cur2, v2);
            Assert.Equal(cur2, v3);
            Assert.Equal(expectedBits, dec.TotalBitsRead);
        }

        [Fact]
        public void Dispose_RespectsLeaveOpen_False_DisposesReader()
        {
            var b = new BitStreamBuilder();
            b.AddBits(0UL, 64); // first only

            var reader = new TestBitReader(b.ToBitArray());
            var dec = new ChimpDecoder(reader, leaveOpen: false);

            _ = dec.ReadNext();
            dec.Dispose();

            Assert.True(reader.IsDisposed);
        }

        [Fact]
        public async Task DisposeAsync_RespectsLeaveOpen_True_DoesNotDisposeReader()
        {
            var b = new BitStreamBuilder();
            b.AddBits(0UL, 64);

            var reader = new TestBitReader(b.ToBitArray());
            var dec = new ChimpDecoder(reader, leaveOpen: true);

            _ = dec.ReadNext();
            await dec.DisposeAsync();

            Assert.False(reader.IsDisposed);
        }
    }
}
