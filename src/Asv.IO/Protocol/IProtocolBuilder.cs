using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using System.Linq;
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
    IProtocolFeatureBuilder Features { get; }
    IProtocolMessageFormatterBuilder Formatters { get; }
    IProtocolParserBuilder Protocols { get; }
    IProtocolPortBuilder PortTypes { get; }
    void ClearAll();
}



internal class ProtocolBuilder : IProtocolBuilder,IProtocolFeatureBuilder,IProtocolMessageFormatterBuilder,IProtocolParserBuilder,IProtocolPortBuilder
{
    private ILoggerFactory _loggerFactory;
    private TimeProvider _timeProvider;
    private IMeterFactory _meterFactory;
    private readonly ImmutableArray<IProtocolFeature>.Builder _featureBuilder = ImmutableArray.CreateBuilder<IProtocolFeature>();
    private readonly ImmutableDictionary<string, ParserFactoryDelegate>.Builder _parserBuilder = ImmutableDictionary.CreateBuilder<string, ParserFactoryDelegate>();
    private readonly ImmutableArray<ProtocolInfo>.Builder _protocolInfoBuilder = ImmutableArray.CreateBuilder<ProtocolInfo>();
    private readonly ImmutableDictionary<string, PortFactoryDelegate>.Builder _portBuilder = ImmutableDictionary.CreateBuilder<string, PortFactoryDelegate>();
    private readonly ImmutableArray<PortTypeInfo>.Builder _portTypeInfoBuilder = ImmutableArray.CreateBuilder<PortTypeInfo>();
    private readonly List<IProtocolMessageFormatter> _formatters = new();

    internal ProtocolBuilder()
    {
        // default configuration
        _loggerFactory = NullLoggerFactory.Instance;
        _timeProvider = TimeProvider.System;
        _meterFactory = new DefaultMeterFactory();
        this.RegisterSerialPort();
        this.RegisterTcpServerPort();
        this.RegisterTcpClientPort();
        this.RegisterUdpPort();
        this.RegisterSimpleFormatter();
    }

    public void SetLog(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _loggerFactory = loggerFactory;
    }

    public void SetDefaultLog()
    {
        _loggerFactory = NullLoggerFactory.Instance;
    }

    public void SetTimeProvider(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    public void SetDefaultTimeProvider()
    {
        _timeProvider = TimeProvider.System;
    }

    public void SetMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        _meterFactory = meterFactory;
    }

    public void SetDefaultMetrics()
    {
        _meterFactory = new DefaultMeterFactory();
    }

    public IProtocolFeatureBuilder Features => this;
    public IProtocolMessageFormatterBuilder Formatters => this;
    public IProtocolParserBuilder Protocols => this;
    public IProtocolPortBuilder PortTypes => this;

    public void ClearAll()
    {
        Features.Clear();
        Formatters.Clear();
        Protocols.Clear();
        PortTypes.Clear();
        SetDefaultLog();
        SetDefaultMetrics();
        SetDefaultTimeProvider();
    }

    public IProtocolFactory Create()
    {
        return new Protocol(
            _featureBuilder.ToImmutable(),
            _parserBuilder.ToImmutable(),
            _protocolInfoBuilder.ToImmutable(),
            _portBuilder.ToImmutable(),
            _portTypeInfoBuilder.ToImmutable(),
            [.._formatters.OrderBy(x => x.Order)],
            _loggerFactory,
            _timeProvider,
            _meterFactory);
    }

    void IProtocolFeatureBuilder.Clear()
    {
        _featureBuilder.Clear();
    }

    public void Register(PortTypeInfo type, PortFactoryDelegate factory)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(factory);
        _portBuilder.Add(type.Scheme, factory);
        _portTypeInfoBuilder.Add(type);
    }

    public void Register(ProtocolInfo info, ParserFactoryDelegate factory)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(factory);
        _parserBuilder.Add(info.Id, factory);
        _protocolInfoBuilder.Add(info);
    }

    public void Register(IProtocolMessageFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        _formatters.Add(formatter);
    }

    public void Register(IProtocolFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        _featureBuilder.Add(feature);
    }

    void IProtocolMessageFormatterBuilder.Clear()
    {
        _formatters.Clear();
    }

    void IProtocolParserBuilder.Clear()
    {
        _parserBuilder.Clear();
        _protocolInfoBuilder.Clear();
    }

    void IProtocolPortBuilder.Clear()
    {
        _portBuilder.Clear();
        _portTypeInfoBuilder.Clear();
    }
}