using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Asv.Common;

namespace Asv.IO;

public class StreamBitWriter(Stream s, bool leaveOpen = false) : AsyncDisposableOnce, IBitWriter
{
    private readonly Stream _s = s ?? throw new ArgumentNullException(nameof(s));
    private byte _buf;
    private int _filled; // 0..7

    public long TotalBitsWritten { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBit(int bit)
    {
        ThrowIfDisposed();
        if ((uint)bit > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(bit));
        }

        if (bit != 0)
        {
            _buf |= (byte)(1 << (7 - _filled));
        }

        _filled++;
        TotalBitsWritten++;
        if (_filled == 8)
        {
            FlushCurrentByte();
        }
    }

    public void WriteBits(ulong value, int count)
    {
        ThrowIfDisposed();
        if (count is < 0 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        for (var i = count - 1; i >= 0; i--)
        {
            WriteBit(((value >> i) & 1UL) != 0 ? 1 : 0);
        }
    }

    public void AlignToByte()
    {
        ThrowIfDisposed();
        if (_filled > 0)
        {
            FlushCurrentByte();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FlushCurrentByte()
    {
        _s.WriteByte(_buf);
        _buf = 0;
        _filled = 0;
    }

    public void Flush() => Flush(false);

    private void Flush(bool idDisposing)
    {
        if (!idDisposing)
        {
            ThrowIfDisposed();
        }
        if (_filled > 0)
        {
            FlushCurrentByte();
        }

        _s.Flush();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Flush(true);
            if (!leaveOpen)
            {
                _s.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        Flush(true);
        if (!leaveOpen)
        {
            await _s.DisposeAsync();
        }
        await base.DisposeAsyncCore();
    }
}
