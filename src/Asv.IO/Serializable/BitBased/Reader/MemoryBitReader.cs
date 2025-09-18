using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using Asv.Common;

namespace Asv.IO;

public sealed class MemoryBitReader(ReadOnlyMemory<byte> buffer) : AsyncDisposableOnce, IBitReader
{
    private int _bytePos = 0; // индекс следующего байта для подхвата
    private int _bitPos = 8; // позиция бита в _cur [0..7]; 8 => «нужно взять новый байт»
    private byte _cur; // текущий подхваченный байт

    private long _totalBitsRead;
    public long TotalBitsRead => _totalBitsRead;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadBit()
    {
        if (_bitPos == 8)
        {
            if (_bytePos >= buffer.Length)
            {
                throw new EndOfStreamException(
                    "MemoryBitReader: attempted to read past end of buffer."
                );
            }

            _cur = buffer.Span[_bytePos++];
            _bitPos = 0;
        }
        int b = (_cur >> (7 - _bitPos)) & 1;
        _bitPos++;
        _totalBitsRead++;
        return b;
    }

    public ulong ReadBits(int count)
    {
        if ((uint)count > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        // Быстрый путь: выровнены по байту и длина кратна 8
        if (_bitPos == 8 && (count & 7) == 0)
        {
            int bytesNeeded = count >> 3;
            if (_bytePos + bytesNeeded > buffer.Length)
            {
                throw new EndOfStreamException("BitReader: not enough data for ReadBits.");
            }

            ulong v = 0;
            var span = buffer.Span.Slice(_bytePos, bytesNeeded);
            for (int i = 0; i < bytesNeeded; i++)
            {
                v = (v << 8) | span[i];
            }

            _bytePos += bytesNeeded;
            _totalBitsRead += (long)count;
            return v;
        }

        // Общий путь: по одному биту
        ulong value = 0;
        for (int i = 0; i < count; i++)
        {
            value = (value << 1) | (uint)ReadBit();
        }

        return value;
    }

    public void AlignToByte()
    {
        if (_bitPos != 8)
        {
            _totalBitsRead += 8 - _bitPos;
            _bitPos = 8;
        }
    }
}
