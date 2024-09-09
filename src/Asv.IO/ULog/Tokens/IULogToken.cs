using System;
using System.Buffers;
using System.IO;

namespace Asv.IO;

public interface IULogToken
{
    string Name { get; }
    ULogToken Type { get; }
    bool TryRead(ReadOnlySequence<byte> data);
    void WriteTo(IBufferWriter<byte> writer);
}




