using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Channels;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Asv.IO;

public class PortArgs
{
    public PortArgs()
    {
    }

    public PortArgs(string? path, NameValueCollection query)
    {
        Path = path;
        Query = query;
    }

    public string? UserInfo { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? Path { get; set; }
    public NameValueCollection Query { get; set; }
}

public delegate IProtocolPort PortFactoryDelegate(
    PortArgs args,
    ImmutableArray<ProtocolInfo> selectedProtocols,
    IProtocolContext context, 
    IStatisticHandler statistic);

public delegate IProtocolParser ParserFactoryDelegate(IProtocolContext context, IStatisticHandler statistic);

public interface IProtocolBuilder
{
    void SetLog(ILoggerFactory loggerFactory);
    void SetTimeProvider(TimeProvider timeProvider);
    void SetMetrics(IMeterFactory meterFactory);
    void ClearFeatures();
    void EnableFeature(IProtocolFeature feature);
    void ClearPrinters();
    void AddPrinter(IProtocolMessageFormatter formatter);
    void ClearProtocols();
    void RegisterProtocol(ProtocolInfo info, ParserFactoryDelegate factory);
    void ClearPorts();
    void RegisterPortType(PortTypeInfo type, PortFactoryDelegate factory);
    void SetRxQueueOptions(int rxQueueSize, bool dropMessageWhenFullRxQueue);
}

public sealed class ProtocolBuilder : IProtocolBuilder
{
    private ILoggerFactory? _loggerFactory;
    private TimeProvider? _timeProvider;
    private IMeterFactory? _meterFactory;
    private readonly ImmutableArray<IProtocolFeature>.Builder _featureBuilder = ImmutableArray.CreateBuilder<IProtocolFeature>();
    private readonly ImmutableDictionary<string, ParserFactoryDelegate>.Builder _parserBuilder = ImmutableDictionary.CreateBuilder<string, ParserFactoryDelegate>();
    private readonly ImmutableArray<ProtocolInfo>.Builder _protocolInfoBuilder = ImmutableArray.CreateBuilder<ProtocolInfo>();
    private readonly ImmutableDictionary<string, PortFactoryDelegate>.Builder _portBuilder = ImmutableDictionary.CreateBuilder<string, PortFactoryDelegate>();
    private readonly ImmutableArray<PortTypeInfo>.Builder _portTypeInfoBuilder = ImmutableArray.CreateBuilder<PortTypeInfo>();
    private readonly List<IProtocolMessageFormatter> _printers = new();
    private int _rxQueueSize;
    private bool _dropMessageWhenFullRxQueue;

    internal ProtocolBuilder()
    {
        // default configuration
        
        this.RegisterSerialPort();
        this.RegisterTcpServerPort();
        this.RegisterTcpClientPort();
        this.RegisterUdpPort();
        
        this.EnableBroadcastFeature();
        
    }

    public void SetLog(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public void SetTimeProvider(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public void SetMetrics(IMeterFactory meterFactory)
    {
        _meterFactory = meterFactory;
    }

    public void ClearFeatures()
    {
        _featureBuilder.Clear();
    }
    
    public void EnableFeature(IProtocolFeature feature)
    {
        _featureBuilder.Add(feature);        
    }
    public void ClearPrinters()
    {
        _printers.Clear();
    }
    public void AddPrinter(IProtocolMessageFormatter formatter)
    {
        _printers.Add(formatter);        
    }

    public void ClearProtocols()
    {
        _parserBuilder.Clear();
        _protocolInfoBuilder.Clear();
    }
    public void RegisterProtocol(ProtocolInfo info, ParserFactoryDelegate factory)
    {
        _parserBuilder.Add(info.Id, factory);
        _protocolInfoBuilder.Add(info);
    }

    public void ClearPorts()
    {
        _portBuilder.Clear();
        _portTypeInfoBuilder.Clear();
    }

    public void RegisterPortType(PortTypeInfo type, PortFactoryDelegate factory)
    {
        _portBuilder.Add(type.Scheme, factory);
        _portTypeInfoBuilder.Add(type);
    }

    public void SetRxQueueOptions(int rxQueueSize, bool dropMessageWhenFullRxQueue)
    {
        _rxQueueSize = rxQueueSize;
        _dropMessageWhenFullRxQueue = dropMessageWhenFullRxQueue;
    }
    
    public IProtocol Build()
    {
        var rxChannel = _rxQueueSize <= 0 
            ? Channel.CreateUnbounded<IProtocolMessage>() 
            : Channel.CreateBounded<IProtocolMessage>(new BoundedChannelOptions(_rxQueueSize)
            {
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = false,
                FullMode = _dropMessageWhenFullRxQueue ? BoundedChannelFullMode.DropOldest: BoundedChannelFullMode.Wait
            });
        var errorChannel = _rxQueueSize <= 0 
            ? Channel.CreateUnbounded<ProtocolException>() 
            : Channel.CreateBounded<ProtocolException>(new BoundedChannelOptions(_rxQueueSize)
            {
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = false,
                FullMode = _dropMessageWhenFullRxQueue ? BoundedChannelFullMode.DropOldest: BoundedChannelFullMode.Wait
            });
        
        return new Protocol(
            rxChannel,
            errorChannel,
            _featureBuilder.ToImmutable(),
            _parserBuilder.ToImmutable(),
            _protocolInfoBuilder.ToImmutable(),
            _portBuilder.ToImmutable(),
            _portTypeInfoBuilder.ToImmutable(),
            [.._printers.OrderBy(x => x.Order)],
            _loggerFactory ?? NullLoggerFactory.Instance,
            _timeProvider ?? TimeProvider.System,
            _meterFactory ?? new DefaultMeterFactory());
    }
}