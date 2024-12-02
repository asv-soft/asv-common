using System;
using System.Diagnostics.Metrics;
using Asv.Cfg;
using Microsoft.Extensions.Logging;

namespace Asv.IO;

public interface IServiceContext
{
    IProtocolConnection Connection { get; }
    ILoggerFactory Log { get; }
    TimeProvider TimeProvider { get; }
    IMeterFactory Metrics { get; }
}

public interface IClientService
{
    
}

public interface IClientServiceBuilder
{
    void SetConfiguration(IConfiguration configuration);
    
}
