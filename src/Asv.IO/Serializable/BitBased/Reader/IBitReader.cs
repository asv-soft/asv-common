using System;
using System.Buffers;

namespace Asv.IO;

public interface IBitReader : IAsyncDisposable, IDisposable
{
    long TotalBitsRead { get; }
    int ReadBit();
    ulong ReadBits(int count);
    void AlignToByte();
}
