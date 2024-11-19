using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using R3;

namespace Asv.IO;

public interface IProtocolParser:IDisposable,IAsyncDisposable
{
    uint StatRxBytes { get; }
    uint StatRxMessages { get; }
    ProtocolParserInfo Info { get; }
    ProtocolTags Tags { get; }
    Observable<IProtocolMessage> OnMessage { get; }
    bool Push(byte data);
    void Reset();
}

