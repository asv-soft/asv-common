using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using System.Linq;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Asv.IO;


public interface IProtocolBuilderEx:IProtocolBuilder
{
    IProtocolFactory Create();
}

public interface IProtocolBuilder
{
    IProtocolBuilder SetLog(ILoggerFactory loggerFactory);
    IProtocolBuilder SetDefaultLog();
    IProtocolBuilder SetTimeProvider(TimeProvider timeProvider);
    IProtocolBuilder SetDefaultTimeProvider();
    IProtocolBuilder SetMetrics(IMeterFactory meterFactory);
    IProtocolBuilder SetDefaultMetrics();
    IProtocolBuilder ClearFeatures();
    IProtocolBuilder RegisterFeature(IProtocolFeature feature);
    IProtocolBuilder ClearFromatter();
    IProtocolBuilder RegisterFormatter(IProtocolMessageFormatter formatter);
    IProtocolBuilder ClearProtocols();
    IProtocolBuilder RegisterProtocol(ProtocolInfo info, ParserFactoryDelegate factory);
    IProtocolBuilder ClearPort();
    IProtocolBuilder RegisterPort(PortTypeInfo type, PortFactoryDelegate factory);
    
    public IProtocolBuilder ClearAll()
    {
        ClearFeatures();
        ClearFromatter();
        ClearProtocols();
        ClearPort();
        return this;
    }
}

public sealed class ProtocolBuilder : IProtocolBuilderEx
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

    public IProtocolBuilder SetLog(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        return this;
    }

    public IProtocolBuilder SetDefaultLog()
    {
        _loggerFactory = NullLoggerFactory.Instance;
        return this;
    }

    public IProtocolBuilder SetTimeProvider(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        return this;
    }

    public IProtocolBuilder SetDefaultTimeProvider()
    {
        _timeProvider = TimeProvider.System;
        return this;
    }

    public IProtocolBuilder SetMetrics(IMeterFactory meterFactory)
    {
        _meterFactory = meterFactory;
        return this;
    }

    public IProtocolBuilder SetDefaultMetrics()
    {
        _meterFactory = new DefaultMeterFactory();
        return this;
    }

    public IProtocolBuilder ClearFeatures()
    {
        _featureBuilder.Clear();
        return this;
    }
    
    public IProtocolBuilder RegisterFeature(IProtocolFeature feature)
    {
        _featureBuilder.Add(feature); 
        return this;
    }
    public IProtocolBuilder ClearFromatter()
    {
        _printers.Clear();
        return this;
    }
    public IProtocolBuilder RegisterFormatter(IProtocolMessageFormatter formatter)
    {
        _printers.Add(formatter);    
        return this;
    }

    public IProtocolBuilder ClearProtocols()
    {
        _parserBuilder.Clear();
        _protocolInfoBuilder.Clear();
        return this;
    }
    public IProtocolBuilder RegisterProtocol(ProtocolInfo info, ParserFactoryDelegate factory)
    {
        _parserBuilder.Add(info.Id, factory);
        _protocolInfoBuilder.Add(info);
        return this;
    }

    public IProtocolBuilder ClearPort()
    {
        _portBuilder.Clear();
        _portTypeInfoBuilder.Clear();
        return this;
    }

    public IProtocolBuilder RegisterPort(PortTypeInfo type, PortFactoryDelegate factory)
    {
        _portBuilder.Add(type.Scheme, factory);
        _portTypeInfoBuilder.Add(type);
        return this;
    }
    
    public IProtocolFactory Create()
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