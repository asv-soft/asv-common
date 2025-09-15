using System;
using System.IO;
using System.Numerics;

namespace Asv.IO;

/// <summary>
/// Формат:
///   - первый sample: 64 бита как есть
///   - далее для каждого значения:
///       0        : повтор предыдущего
///       10 + P   : reuse window, пишем только payload (W бит)
///       11 + L5 + (W-1)6 + P(W бит): объявляем новое окно и payload
/// где окно задаётся парой {L, W}, trailing = 64 - L - W.
/// </summary>
public sealed class ChimpValueEncoder(IBitWriter writer, bool leaveOpen = false) : IDisposable
{
    private bool _first = true;
    private ulong _prevBits;

    private int _l = -1; // leading zeros sticky
    private int _w = -1; // width of significant bits sticky

    public long TotalBitsWritten => writer.TotalBitsWritten;

    public void Add(ulong bits)
    {
        if (_first)
        {
            writer.WriteBits(bits, 64);
            _prevBits = bits;
            _first = false;
            return;
        }

        var xor = _prevBits ^ bits;
        if (xor == 0)
        {
            writer.WriteBit(0); // repeat
            _prevBits = bits;
            return;
        }

        var leading = BitOperations.LeadingZeroCount(xor);
        var trailing = BitOperations.TrailingZeroCount(xor);
        var sig = 64 - leading - trailing;
        if (sig <= 0)
        {
            sig = 1;
        }

        var reuse = (_l >= 0 && _w > 0) && (leading >= _l) && (sig <= _w);
        if (reuse)
        {
            writer.WriteBits(0b10, 2);
            var tWin = 64 - _l - _w;
            var payload = (_w == 64) ? xor : ((xor >> tWin) & ((1UL << _w) - 1));
            writer.WriteBits(payload, _w);
        }
        else
        {
            var l5 = Math.Min(leading, 31);
            var w = Math.Min(Math.Max(sig, 1), 64);

            writer.WriteBits(0b11, 2);
            writer.WriteBits((ulong)l5, 5);
            writer.WriteBits((ulong)(w - 1), 6);

            var tWin = 64 - l5 - w;
            var payload = (w == 64) ? xor : ((xor >> tWin) & ((1UL << w) - 1));
            writer.WriteBits(payload, w);

            _l = l5;
            _w = w;
        }

        _prevBits = bits;
    }

    public void Dispose()
    {
        if (!leaveOpen)
        {
            writer.Dispose();
        }
    }
}
