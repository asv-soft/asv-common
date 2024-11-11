using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Asv.IO;

public class PipeEndpointConfig:PipeConfigBase
{
    public int ProcessIntervalMs { get; set; } = 30;
    public bool UseSynchronizationContext { get; set; } = false;
    public long PauseWriterThreshold { get; set; } = -1;
    public long ResumeWriterThreshold { get; set; } = -1;
    public int MinimumSegmentSize { get; set; } = -1;
    public override bool TryValidate(out string? error)
    {
        if (ProcessIntervalMs <= 0)
        {
            error = $"{nameof(ProcessIntervalMs)} must be greater than 0";
            return false;
        }
        if (PauseWriterThreshold < 0)
        {
            error = $"{nameof(PauseWriterThreshold)} must be greater than or equal to 0";
            return false;
        }
        if (ResumeWriterThreshold < 0)
        {
            error = $"{nameof(ResumeWriterThreshold)} must be greater than or equal to 0";
            return false;
        }
        if (MinimumSegmentSize < 0)
        {
            error = $"{nameof(MinimumSegmentSize)} must be greater than or equal to 0";
            return false;
        }
        error = null;
        return true;
    }
}
public abstract class PipeEndpoint : IPipeEndpoint
{
    private readonly PipeEndpointConfig _config;
    private readonly Pipe _input;
    private readonly Pipe _output;
    private readonly ILogger<PipeEndpoint> _logger;
    private readonly ITimer _rwTimer;
    private readonly CancellationTokenSource _disposeCancel = new();
    private volatile int _isDisposed;
    private volatile int _loopIsBusy;

    protected PipeEndpoint(IPipePort parent, PipeEndpointConfig config, IPipeCore core)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(core);
        config.Validate();
        Parent = parent;
        _config = config;
        _logger = core.LoggerFactory.CreateLogger<PipeEndpoint>();
        var options = new PipeOptions(
            pauseWriterThreshold: _config.PauseWriterThreshold,
            resumeWriterThreshold: _config.ResumeWriterThreshold,
            useSynchronizationContext:_config.UseSynchronizationContext,
            minimumSegmentSize:_config.MinimumSegmentSize);
        _input = new Pipe(options);
        _output = new Pipe(options);
        _rwTimer = core.TimeProvider.CreateTimer(ProcessingLoop, null,
            TimeSpan.FromMilliseconds(config.ProcessIntervalMs), TimeSpan.FromMilliseconds(config.ProcessIntervalMs));
    }
    protected abstract Task InternalWrite(PipeReader rdr, CancellationToken cancel);
    protected abstract Task InternalRead(PipeWriter wrt, CancellationToken cancel);
    public PipeReader Input => _input.Reader;
    public PipeWriter Output => _output.Writer;
    public TagList Tags { get; } = [];
    public abstract string Id { get; }
    public IPipePort Parent { get; }
    private void ProcessingLoop(object? state)
    {
        if (Interlocked.CompareExchange(ref _loopIsBusy, 1, 0) == 1)
        {
            _logger.ZLogTrace($"Skip Read/Write loop {_config.ProcessIntervalMs} ms");
            return;
        }
        try
        {
            Task.WaitAll(InternalRead(_input.Writer, _disposeCancel.Token), InternalWrite(_output.Reader, _disposeCancel.Token));
        }
        catch (Exception e)
        {
            _logger.ZLogError($"Error in Read/Write loop: {e.Message}");
            Dispose();
        }
        finally
        {
            Interlocked.Exchange(ref _loopIsBusy, 0);
        }
    }
    
    #region Dispose
    public bool IsDisposed => _isDisposed != 0;
    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
        {
            _logger.LogTrace("Duplicate dispose call");
            return;
        }
        if (disposing)
        {
            _input.Reader.Complete();
            _input.Writer.Complete();
            _output.Reader.Complete();
            _output.Reader.Complete();
            _rwTimer.Dispose();
            _disposeCancel.Cancel(false);
            _disposeCancel.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
        {
            _logger.LogTrace("Duplicate dispose call");
            return;
        }
        await _input.Reader.CompleteAsync();
        await _input.Writer.CompleteAsync();
        await _output.Reader.CompleteAsync();
        await _output.Reader.CompleteAsync();
        await _rwTimer.DisposeAsync();
        _disposeCancel.Cancel(false);
        if (_disposeCancel is IAsyncDisposable disposeCancelAsyncDisposable)
            await disposeCancelAsyncDisposable.DisposeAsync();
        else
            _disposeCancel.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    #endregion
}