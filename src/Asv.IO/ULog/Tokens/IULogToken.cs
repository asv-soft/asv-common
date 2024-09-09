using System;
using System.Buffers;
using System.IO;

namespace Asv.IO;

public interface IULogToken:ISpanSerializable
{
    string Name { get; }
    ULogToken Type { get; }
    
}




