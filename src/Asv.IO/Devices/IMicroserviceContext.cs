using System;
using System.Diagnostics.Metrics;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Asv.IO;

public interface IMicroserviceContext
{
    IProtocolConnection Connection { get; }
    ILoggerFactory LoggerFactory { get; }
    TimeProvider TimeProvider { get; }
    IMeterFactory Metrics { get; }
}

public class MicroserviceContext : IMicroserviceContext
{
    public MicroserviceContext(
        IProtocolConnection connection,
        ILoggerFactory? loggerFactory = null,
        TimeProvider? timeProvider = null,
        IMeterFactory? metrics = null
    )
    {
        Connection = connection;
        LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        TimeProvider = timeProvider ?? TimeProvider.System;
        Metrics = metrics ?? new DefaultMeterFactory();
    }

    public MicroserviceContext(IMicroserviceContext context)
    {
        Connection = context.Connection;
        LoggerFactory = context.LoggerFactory;
        TimeProvider = context.TimeProvider;
        Metrics = context.Metrics;
    }

    public IProtocolConnection Connection { get; }
    public ILoggerFactory LoggerFactory { get; }
    public TimeProvider TimeProvider { get; }
    public IMeterFactory Metrics { get; }
}
