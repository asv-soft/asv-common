using System;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;

namespace Asv.IO;

public class ProtocolMessageTransponder<TMessage>
    : AsyncDisposableWithCancel,
        IProtocolMessageTransponder<TMessage>
    where TMessage : IProtocolMessage
{
    private readonly TMessage _message;
    private readonly Action<TMessage> _everySendCallback;
    private readonly IProtocolConnection _connection;
    private readonly TimeProvider _timeProvider;
    private readonly Lock _sync = new();
    private ITimer? _timer;
    private readonly ReactiveProperty<TransponderState> _state = new();
    private readonly SemaphoreSlim _dataLock = new(1, 1);
    private readonly CancellationToken _lifetime;
    private Task _sendTask = Task.CompletedTask;
    private readonly ILogger<ProtocolMessageTransponder<TMessage>> _logger;

    public ProtocolMessageTransponder(
        TMessage message,
        Action<TMessage>? everySendCallback,
        IProtocolConnection connection,
        TimeProvider timeProvider,
        ILoggerFactory loggerFactory
    )
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(everySendCallback);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _message = message;
        _everySendCallback = everySendCallback;
        _connection = connection;
        _timeProvider = timeProvider;
        _lifetime = DisposeCancel;
        _logger = loggerFactory.CreateLogger<ProtocolMessageTransponder<TMessage>>();
    }

    public void Start(TimeSpan dueTime, TimeSpan period)
    {
        using (_sync.EnterScope())
        {
            ThrowIfDisposed();
            _timer?.Dispose();
            IsStarted = true;
            _timer = _timeProvider.CreateTimer(OnTick, null, dueTime, period);
        }
    }

    private void OnTick(object? state)
    {
        TaskCompletionSource completion;
        using (_sync.EnterScope())
        {
            if (IsDisposed || !IsStarted)
            {
                return;
            }

            if (!_sendTask.IsCompleted)
            {
                LogSkipped();
                return;
            }

            completion = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            _sendTask = completion.Task;
        }

        _ = SendAsync(completion);
    }

    private async Task SendAsync(TaskCompletionSource completion)
    {
        var entered = false;
        try
        {
            await _dataLock.WaitAsync(_lifetime).ConfigureAwait(false);
            entered = true;
            _everySendCallback(_message);
            _lifetime.ThrowIfCancellationRequested();
            await _connection.Send(_message, _lifetime).ConfigureAwait(false);
            LogSuccess();
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception e)
        {
            LogError(e);
        }
        finally
        {
            if (entered)
            {
                _dataLock.Release();
            }
            completion.TrySetResult();
        }
    }

    private void LogError(Exception e)
    {
        using (_sync.EnterScope())
        {
            if (IsDisposed || _state.Value == TransponderState.ErrorToSend)
            {
                return;
            }

            _state.Value = TransponderState.ErrorToSend;
            _logger.ZLogError($"{_message.Name} sending error:{e.Message}");
        }
    }

    private void LogSuccess()
    {
        using (_sync.EnterScope())
        {
            if (IsDisposed || _state.Value == TransponderState.Ok)
            {
                return;
            }

            _state.Value = TransponderState.Ok;
            _logger.ZLogDebug($"{_message.Name} start stream");
        }
    }

    private void LogSkipped()
    {
        if (_state.Value == TransponderState.Skipped)
        {
            return;
        }

        _state.Value = TransponderState.Skipped;
        _logger.ZLogWarning(
            $"{_message.Name} skipped sending: previous command has not yet been executed"
        );
    }

    public bool IsStarted { get; private set; }
    public ReadOnlyReactiveProperty<TransponderState> State => _state;

    public void Stop()
    {
        ITimer? timer;
        using (_sync.EnterScope())
        {
            timer = _timer;
            _timer = null;
            IsStarted = false;
        }
        timer?.Dispose();
    }

    public void Set(Action<TMessage> changeCallback)
    {
        ArgumentNullException.ThrowIfNull(changeCallback);
        ThrowIfDisposed();
        _dataLock.Wait(_lifetime);
        try
        {
            ThrowIfDisposed();
            changeCallback(_message);
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, $"Error to set new value for {_message.Name}:{e.Message}");
        }
        finally
        {
            _dataLock.Release();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Stop();
            base.Dispose(disposing);

            // Synchronous disposal may run inside a send callback. Cleanup waits without blocking it.
            _ = DisposeResourcesAsync();
            return;
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        Stop();
        await base.DisposeAsyncCore().ConfigureAwait(false);
        await DisposeResourcesAsync().ConfigureAwait(false);
    }

    private async Task DisposeResourcesAsync()
    {
        Task pending;
        using (_sync.EnterScope())
        {
            pending = _sendTask;
        }
        await pending.ConfigureAwait(false);
        await _dataLock.WaitAsync().ConfigureAwait(false);
        _state.Dispose();
        _dataLock.Dispose();
    }
}
