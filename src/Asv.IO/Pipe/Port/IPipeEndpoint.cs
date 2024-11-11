using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Asv.IO;

public interface IPipeEndpoint : IDuplexPipe
{
    TagList Tags { get; }
}

public class PipeEndpointConfig
{
    public int RwLoopIntervalMs { get; set; } = 30;
}
public abstract class PipeEndpoint : IPipeEndpoint
{
    private readonly PipeEndpointConfig _config;
    private readonly Pipe _input;
    private readonly Pipe _output;
    private readonly ILogger<PipeEndpoint> _logger;
    private int _loopIsBusy;
    private readonly ITimer _rwTimer;
    private readonly CancellationTokenSource _disposeCancel = new();

    protected PipeEndpoint(PipeEndpointConfig config, IPipeCore core)
    {
        _config = config;
        Tags = new TagList();
        _logger = core.LoggerFactory.CreateLogger<PipeEndpoint>();
        _input = new Pipe();
        _output = new Pipe();
        _rwTimer = core.TimeProvider.CreateTimer(ReadWriteLoop, null,
            TimeSpan.FromMilliseconds(config.RwLoopIntervalMs), TimeSpan.FromMilliseconds(config.RwLoopIntervalMs));
    }

    private async void ReadWriteLoop(object? state)
    {
        if (Interlocked.CompareExchange(ref _loopIsBusy, 1, 0) == 1)
        {
            _logger.ZLogTrace($"Skip Read/Write loop {_config.RwLoopIntervalMs} ms");
            return;
        }
        try
        {
            await InternalRead(_input.Writer,_disposeCancel.Token);
            await InternalWrite(_output.Reader,_disposeCancel.Token);
        }
        catch (Exception e)
        {
            _logger.ZLogError($"Error in Read/Write loop: {e.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _loopIsBusy, 0);
        }
    }

    protected abstract Task InternalWrite(PipeReader outputReader, CancellationToken cancel);
    protected abstract Task InternalRead(PipeWriter inputWriter, CancellationToken cancel);

    public PipeReader Input => _input.Reader;
    public PipeWriter Output => _output.Writer;
    public TagList Tags { get; }
}