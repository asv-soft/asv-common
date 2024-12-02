using System;
using System.Diagnostics.Metrics;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Asv.IO;

public interface IDeviceContext
{
    IProtocolConnection Connection { get; }
    ILoggerFactory LoggerFactory { get; }
    TimeProvider TimeProvider { get; }
    IMeterFactory Metrics { get; }
}

public class DeviceContext(
    IProtocolConnection connection,
    ILoggerFactory? loggerFactory = null,
    TimeProvider? timeProvider = null,
    IMeterFactory? metrics = null)
    : IDeviceContext
{
    public IProtocolConnection Connection { get; } = connection;
    public ILoggerFactory LoggerFactory { get; } = loggerFactory ?? NullLoggerFactory.Instance;
    public TimeProvider TimeProvider { get; } = timeProvider ?? TimeProvider.System;
    public IMeterFactory Metrics { get; } = metrics ?? new DefaultMeterFactory();
}

