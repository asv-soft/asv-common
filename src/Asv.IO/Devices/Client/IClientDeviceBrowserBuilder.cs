using System;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Asv.IO;

public interface IClientDeviceBrowserBuilder
{
    void SetLog(ILoggerFactory loggerFactory);
    void SetDefaultLog();
    void SetTimeProvider(TimeProvider timeProvider);
    void SetDefaultTimeProvider();
    void SetMetrics(IMeterFactory meterFactory);
    void SetDefaultMetrics();
    void SetConfig(ClientDeviceBrowserConfig config);
    void SetDefaultConfig();
    IClientDeviceExtenderBuilder Extenders { get; }
    IClientDeviceFactoryBuilder Factories { get; }
}