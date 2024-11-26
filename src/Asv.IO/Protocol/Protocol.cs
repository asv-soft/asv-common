using System;
using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Asv.IO;

public class Protocol: IProtocolFactory, IProtocolContext
{
    #region Static

    public static IProtocolFactory Create(Action<IProtocolBuilder> configure)
    {
        var builder = new ProtocolBuilder();
        configure(builder);
        return builder.Build();
    }
    
    #endregion

    internal Protocol(
        ImmutableArray<IProtocolFeature> features, 
        ImmutableDictionary<string,ParserFactoryDelegate> parserFactory, 
        ImmutableArray<ProtocolInfo> availableProtocols, 
        ImmutableDictionary<string,PortFactoryDelegate> portFactory, 
        ImmutableArray<PortTypeInfo> availablePortTypes,
        ImmutableArray<IProtocolMessageFormatter> formatters, 
        ILoggerFactory loggerFactory, 
        TimeProvider timeProvider, 
        IMeterFactory meterFactory)
    {
        Features = features;
        ParserFactory = parserFactory;
        AvailableProtocols = availableProtocols;
        PortFactory = portFactory;
        AvailablePortTypes = availablePortTypes;
        Formatters = formatters;
        LoggerFactory = loggerFactory;
        TimeProvider = timeProvider;
        MeterFactory = meterFactory;
    }

    public ImmutableArray<PortTypeInfo> AvailablePortTypes { get; }
    public ILoggerFactory LoggerFactory { get; }
    public TimeProvider TimeProvider { get; }
    public IMeterFactory MeterFactory { get; }
    public ImmutableDictionary<string, ParserFactoryDelegate> ParserFactory { get; }
    public ImmutableArray<IProtocolFeature> Features { get; }
    public ImmutableArray<ProtocolInfo> AvailableProtocols { get; }
    public ImmutableDictionary<string, PortFactoryDelegate> PortFactory { get; }
    public ImmutableArray<IProtocolMessageFormatter> Formatters { get; }

    public IProtocolRouter CreateRouter(string id)
    {
        return new ProtocolRouter(ProtocolHelper.NormalizeId(id), this);
    }
    
    public IVirtualConnection CreateVirtualConnection(Func<IProtocolMessage, bool>? clientToServerFilter = null,Func<IProtocolMessage, bool>? serverToClientFilter = null)
    {
        return new VirtualConnection(clientToServerFilter,serverToClientFilter,this);
    }
}