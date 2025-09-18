using System;

namespace Asv.IO;

public interface IBitWriter : IAsyncDisposable, IDisposable
{
    long TotalBitsWritten { get; }
    void WriteBit(int bit);
    void WriteBits(ulong value, int count);
    void AlignToByte();
    void Flush();
}
