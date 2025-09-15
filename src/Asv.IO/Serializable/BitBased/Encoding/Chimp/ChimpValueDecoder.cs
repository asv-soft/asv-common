using System;
using System.Buffers;
using System.IO;

namespace Asv.IO;

public sealed class ChimpValueDecoder(IBitReader input)
{
    private bool _first = true;
    private ulong _prevBits;
    private int _l = -1;
    private int _w = -1;

    public long TotalBitsRead => input.TotalBitsRead;

    public ulong ReadNext()
    {
        if (_first)
        {
            var b0 = input.ReadBits(64);
            _prevBits = b0;
            _first = false;
            return b0;
        }

        var p1 = input.ReadBit();
        if (p1 == 0)
        {
            return _prevBits;
        }

        var p2 = input.ReadBit();
        if (p2 == 0)
        {
            if (_l < 0 || _w <= 0)
            {
                throw new InvalidDataException("Reuse before window defined.");
            }

            var tWin = 64 - _l - _w;
            var payload = input.ReadBits(_w);
            var xor = (_w == 64) ? payload : (payload << tWin);
            var cur = _prevBits ^ xor;
            _prevBits = cur;
            return cur;
        }
        else
        {
            var L = (int)input.ReadBits(5);
            var W = (int)input.ReadBits(6) + 1;
            var tWin = 64 - L - W;
            var payload = input.ReadBits(W);
            var xor = (W == 64) ? payload : (payload << tWin);
            var cur = _prevBits ^ xor;
            _prevBits = cur;
            _l = L;
            _w = W;
            return cur;
        }
    }
}
