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



public interface IProtocolBuilder
{
    void SetLog(ILoggerFactory loggerFactory);
    void SetDefaultLog();
    void SetTimeProvider(TimeProvider timeProvider);
    void SetDefaultTimeProvider();
    void SetMetrics(IMeterFactory meterFactory);
    void SetDefaultMetrics();
    void ClearFeatures();
    void RegisterFeature(IProtocolFeature feature);
    void ClearPrinters();
    void RegisterFormatter(IProtocolMessageFormatter formatter);
    void ClearProtocols();
    void RegisterProtocol(ProtocolInfo info, ParserFactoryDelegate factory);
    void ClearPortType();
    void RegisterPortType(PortTypeInfo type, PortFactoryDelegate factory);
    
    public void ClearAll()
    {
        ClearFeatures();
        ClearPrinters();
        ClearProtocols();
        ClearPortType();
    }
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

    internal ProtocolBuilder()
    {
        // default configuration
        
        this.RegisterSerialPort();
        this.RegisterTcpServerPort();
        this.RegisterTcpClientPort();
        this.RegisterUdpPort();
        
    }

    public void SetLog(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public void SetDefaultLog()
    {
        _loggerFactory = NullLoggerFactory.Instance;
    }

    public void SetTimeProvider(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public void SetDefaultTimeProvider()
    {
        _timeProvider = TimeProvider.System;
    }

    public void SetMetrics(IMeterFactory meterFactory)
    {
        _meterFactory = meterFactory;
    }

    public void SetDefaultMetrics()
    {
        throw new NotImplementedException();
    }

    public void ClearFeatures()
    {
        _featureBuilder.Clear();
    }
    
    public void RegisterFeature(IProtocolFeature feature)
    {
        _featureBuilder.Add(feature);        
    }
    public void ClearPrinters()
    {
        _printers.Clear();
    }
    public void RegisterFormatter(IProtocolMessageFormatter formatter)
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

    public void ClearPortType()
    {
        _portBuilder.Clear();
        _portTypeInfoBuilder.Clear();
    }

    public void RegisterPortType(PortTypeInfo type, PortFactoryDelegate factory)
    {
        _portBuilder.Add(type.Scheme, factory);
        _portTypeInfoBuilder.Add(type);
    }
    
    public IProtocolFactory Build()
    {
        return new Protocol(
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