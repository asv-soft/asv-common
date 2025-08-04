using System;
using R3;

namespace Asv.IO;

public delegate IProtocolParser ParserFactoryDelegate(IProtocolContext context, IStatisticHandler? statistic);

public interface IProtocolParser:IDisposable,IAsyncDisposable
{
    IStatistic Statistic { get; }
    ProtocolInfo Info { get; }
    ProtocolTags Tags { get; }
    Observable<IProtocolMessage> OnMessage { get; }
    Observable<Exception> OnError { get; }
    bool Push(byte data);
    void Reset();
}

public interface IProtocolParserBuilder
{
    void Clear();
    void Register(ProtocolInfo info, ParserFactoryDelegate factory);
}


