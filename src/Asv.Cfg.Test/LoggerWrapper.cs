using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;
using R3;
using Xunit.Abstractions;

namespace Asv.Cfg.Test;

public class TestLoggerFactory(ITestOutputHelper testOutputHelper, TimeProvider time, string prefix) : ILoggerFactory
{
    public TimeProvider Time { get; } = time;

    public void Dispose()
    {
        
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(testOutputHelper,Time, $"{prefix}.{categoryName}");
    }

    public void AddProvider(ILoggerProvider provider)
    {
        
    }
}

public class TestLogger : ILogger
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly TimeProvider _time;
    private readonly string? _categoryName;
    private readonly long _start;

    public TestLogger(ITestOutputHelper testOutputHelper, TimeProvider time, string? categoryName)
    {
        _testOutputHelper = testOutputHelper;
        _time = time;
        _categoryName = categoryName;
        _start = _time.GetTimestamp();
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => Disposable.Empty;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        try
        {
            var t = _time.GetElapsedTime(_start);
            _testOutputHelper.WriteLine($"+{t.TotalSeconds:000.000} |{Thread.CurrentThread.ManagedThreadId:00}|={ConvertToStr(logLevel)}=| {_categoryName,-20} | {formatter(state, exception)}");
        }
        catch
        {
            // This can happen when the test is not active
        }
    }

    private string ConvertToStr(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            LogLevel.None => "NON",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
        };
    }
}