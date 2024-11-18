using System;
using System.Diagnostics.Metrics;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Asv.IO;

public interface IProtocolCore
{
    ILoggerFactory LoggerFactory { get; }
    TimeProvider TimeProvider { get; }
    IMeterFactory MeterFactory { get; }
}

public class ProtocolCore : IProtocolCore
{
    public ProtocolCore(ILoggerFactory? loggerFactory = null, TimeProvider? timeProvider = null, IMeterFactory? meterFactory = null)
    {
        LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        TimeProvider = timeProvider ?? TimeProvider.System;
        MeterFactory = meterFactory ?? new DefaultMeterFactory();
    }

    public ILoggerFactory LoggerFactory { get; }
    public TimeProvider TimeProvider { get; }
    public IMeterFactory MeterFactory { get; }
}