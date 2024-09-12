using System;
using System.Buffers;
using System.IO;

namespace Asv.IO;

public interface IULogToken:ISizedSpanSerializable
{
    string Name { get; }
    ULogToken Type { get; }
    TokenPlaceFlags Section { get; }
}




