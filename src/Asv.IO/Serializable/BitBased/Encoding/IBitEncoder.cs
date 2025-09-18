using System;

namespace Asv.IO;

public interface IBitEncoder<T> : IDisposable
{
    long TotalBitsWritten { get; }
    void Add(T bits);
}