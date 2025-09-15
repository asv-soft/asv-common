using System;

namespace Asv.IO;

public interface IBitWriter : IDisposable
{
    long TotalBitsWritten { get; }
    void WriteBit(int bit);
    void WriteBits(ulong value, int count);
    void AlignToByte();
    void Flush();
}
