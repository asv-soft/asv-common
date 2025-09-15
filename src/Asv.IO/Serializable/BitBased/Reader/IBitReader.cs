using System.Buffers;

namespace Asv.IO;

public interface IBitReader
{
    long TotalBitsRead { get; }
    int ReadBit();
    ulong ReadBits(int count);
    void AlignToByte();
}
