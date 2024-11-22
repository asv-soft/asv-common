using System;
using R3;

namespace Asv.IO;

public interface IProtocolParser:IDisposable,IAsyncDisposable
{
    IStatistic Statistic { get; }
    ProtocolInfo Info { get; }
    ProtocolTags Tags { get; }
    Observable<IProtocolMessage> OnMessage { get; }
    bool Push(byte data);
    void Reset();
}


