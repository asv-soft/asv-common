using System;

namespace Asv.IO;

public interface IBitDecoder<T> : IDisposable
{
    long TotalBitsRead { get; }
    T ReadNext();
}
