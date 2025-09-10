using System;
using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Asv.IO;

public interface IProtocolContext : IMessageFormatter
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
