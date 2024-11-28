using System;
using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Asv.IO;


public interface IProtocolContext
{
    ILoggerFactory LoggerFactory { get; }
    TimeProvider TimeProvider { get; }
    IMeterFactory MeterFactory { get; }
    ImmutableDictionary<string, ParserFactoryDelegate> ParserFactory { get; }
    ImmutableArray<IProtocolFeature> Features { get; }
    ImmutableArray<ProtocolInfo> AvailableProtocols { get; }
    ImmutableDictionary<string, PortFactoryDelegate> PortFactory { get; }
    ImmutableArray<PortTypeInfo> AvailablePortTypes { get; }
    ImmutableArray<IProtocolMessageFormatter> Formatters { get; }
}



public interface IProtocolFactory
{
    ILoggerFactory LoggerFactory { get; }
    TimeProvider TimeProvider { get; }
    IMeterFactory MeterFactory { get; }
    ImmutableArray<ProtocolInfo> AvailableProtocols { get; }
    ImmutableArray<PortTypeInfo> AvailablePortTypes { get; }
    ImmutableArray<IProtocolMessageFormatter> Formatters { get; }
    ImmutableArray<IProtocolFeature> Features { get; }
    IStatistic Statistic { get; }
    IProtocolPort CreatePort(string id);
    IProtocolParser CreateParser(string id);
    IProtocolRouter CreateRouter(string id);
    IVirtualConnection CreateVirtualConnection(Func<IProtocolMessage, bool>? clientToServerFilter = null,
        Func<IProtocolMessage, bool>? serverToClientFilter = null);
    
    
}