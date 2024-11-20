using System;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Asv.IO;

public class PortArgs
{
    public string? UserInfo { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? Path { get; set; }
    public NameValueCollection Query { get; set; }
}

public delegate IProtocolPort PortFactoryDelegate(
    PortArgs args,
    ImmutableArray<IProtocolProcessingFeature> features,
    ImmutableArray<ParserFactoryDelegate> parserFactory,
    IProtocolCore core);

public delegate IProtocolParser ParserFactoryDelegate(IProtocolCore core);

public interface IProtocolBuilder
{
    void SetLog(ILoggerFactory loggerFactory);
    void SetTimeProvider(TimeProvider timeProvider);
    void SetMetrics(IMeterFactory meterFactory);
    void AddFeature(IProtocolProcessingFeature feature);
    void RegisterProtocol(ProtocolInfo info, ParserFactoryDelegate factory);
    void RegisterPort(PortTypeInfo type, PortFactoryDelegate factory);
}

public sealed class ProtocolBuilder : IProtocolBuilder
{
    private ILoggerFactory? _loggerFactory;
    private TimeProvider? _timeProvider;
    private IMeterFactory? _meterFactory;
    private readonly ImmutableArray<IProtocolProcessingFeature>.Builder _featureBuilder = ImmutableArray.CreateBuilder<IProtocolProcessingFeature>();
    private readonly ImmutableDictionary<string, ParserFactoryDelegate>.Builder _parserBuilder = ImmutableDictionary.CreateBuilder<string, ParserFactoryDelegate>();
    private readonly ImmutableArray<ProtocolInfo>.Builder _protocolInfoBuilder = ImmutableArray.CreateBuilder<ProtocolInfo>();
    private readonly ImmutableDictionary<string, PortFactoryDelegate>.Builder _portBuilder = ImmutableDictionary.CreateBuilder<string, PortFactoryDelegate>();
    private readonly ImmutableArray<PortTypeInfo>.Builder _portTypeInfoBuilder = ImmutableArray.CreateBuilder<PortTypeInfo>();

    internal ProtocolBuilder()
    {
        
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

    public void AddFeature(IProtocolProcessingFeature feature)
    {
        _featureBuilder.Add(feature);        
    }

    public void RegisterProtocol(ProtocolInfo info, ParserFactoryDelegate factory)
    {
        _parserBuilder.Add(info.Id, factory);
        _protocolInfoBuilder.Add(info);
    }
    
    public void RegisterPort(PortTypeInfo type, PortFactoryDelegate factory)
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
            core);
    }
}