using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics.Metrics;
using System.Linq;
using Microsoft.Extensions.Logging;

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
    ImmutableArray<IProtocolFeature> features,
    ImmutableDictionary<string, ParserFactoryDelegate> parsers,
    ImmutableArray<ProtocolInfo> protocols,
    IProtocolCore core);

public delegate IProtocolParser ParserFactoryDelegate(IProtocolCore core);

public interface IProtocolBuilder
{
    void SetLog(ILoggerFactory loggerFactory);
    void SetTimeProvider(TimeProvider timeProvider);
    void SetMetrics(IMeterFactory meterFactory);
    void ClearFeatures();
    void EnableFeature(IProtocolFeature feature);
    void ClearPrinters();
    void AddPrinter(IProtocolMessagePrinter printer);
    void ClearProtocols();
    void RegisterProtocol(ProtocolInfo info, ParserFactoryDelegate factory);
    void ClearPorts();
    void RegisterPortType(PortTypeInfo type, PortFactoryDelegate factory);
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
    private readonly List<IProtocolMessagePrinter> _printers = new();

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
    public void AddPrinter(IProtocolMessagePrinter printer)
    {
        _printers.Add(printer);        
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

    public IProtocol Build()
    {
        var core = new ProtocolCore(_loggerFactory, _timeProvider, _meterFactory);
        return new Protocol(
            _featureBuilder.ToImmutable(),
            _parserBuilder.ToImmutable(),
            _protocolInfoBuilder.ToImmutable(),
            _portBuilder.ToImmutable(),
            _portTypeInfoBuilder.ToImmutable(),
            [.._printers.OrderBy(x => x.Order)],
            core);
    }
}