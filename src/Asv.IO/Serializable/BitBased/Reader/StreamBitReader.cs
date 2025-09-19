using System;
using System.IO;
using System.Threading.Tasks;
using Asv.Common;

namespace Asv.IO;

public sealed class StreamBitReader(Stream s, bool leaveOpen = false)
    : AsyncDisposableOnce,
        IBitReader
{
    private readonly Stream _s = s ?? throw new ArgumentNullException(nameof(s));
    private int _cur = -1; // текущий байт или -1
    private int _pos = 8; // позиция бита [0..7]; 8 = пусто

    public long TotalBitsRead { get; private set; }

    public int ReadBit()
    {
        ThrowIfDisposed();
        if (_pos == 8)
        {
            _cur = _s.ReadByte();
            if (_cur < 0)
            {
                throw new EndOfStreamException("EOF in ReadBit()");
            }

            _pos = 0;
        }
        var b = (_cur >> (7 - _pos)) & 1;
        _pos++;
        TotalBitsRead++;
        return b;
    }

    public ulong ReadBits(int count)
    {
        ThrowIfDisposed();
        if ((uint)count > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        ulong v = 0;
        for (var i = 0; i < count; i++)
        {
            v = (v << 1) | (ulong)ReadBit();
        }

        return v;
    }

    public void AlignToByte()
    {
        ThrowIfDisposed();
        if (_pos != 8)
        {
            TotalBitsRead += 8 - _pos;
            _pos = 8;
        }
    }

    protected sealed override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!leaveOpen)
            {
                _s.Dispose();
            }
        }
        base.Dispose(disposing);
    }

    protected sealed override async ValueTask DisposeAsyncCore()
    {
        if (!leaveOpen)
        {
            await _s.DisposeAsync();
        }
        await base.DisposeAsyncCore();
    }
}
