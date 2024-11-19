using System;
using R3;

namespace Asv.IO;

public interface IProtocolParser:IDisposable,IAsyncDisposable
{
    uint StatRxBytes { get; }
    uint StatRxMessages { get; }
    ProtocolInfo Info { get; }
    ProtocolTags Tags { get; }
    Observable<IProtocolMessage> OnMessage { get; }
    bool Push(byte data);
    void Reset();
}

