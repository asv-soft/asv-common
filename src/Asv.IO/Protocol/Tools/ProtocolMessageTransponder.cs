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
    private int _isSending;
    private ITimer? _timer;
    private readonly ReactiveProperty<TransponderState> _state = new();
    private readonly ReaderWriterLockSlim _dataLock = new();
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
        _message = message;
        _everySendCallback = everySendCallback;
        _connection = connection;
        _timeProvider = timeProvider;
        _logger = loggerFactory.CreateLogger<ProtocolMessageTransponder<TMessage>>();
    }

    public void Start(TimeSpan dueTime, TimeSpan period)
    {
        using (_sync.EnterScope())
        {
            _timer?.Dispose();
            _timer = _timeProvider.CreateTimer(s => OnTick(s).SafeFireAndForget(), null, dueTime, period);
            IsStarted = true;
        }
    }

    private async Task OnTick(object? state)
    {
        try
        {
            if (Interlocked.CompareExchange(ref _isSending, 1, 0) == 1)
            {
                LogSkipped();
                return;
            }

            _dataLock.EnterReadLock();
            _everySendCallback(_message);
            await _connection.Send(_message, DisposeCancel);
            LogSuccess();
        }
        catch (Exception e)
        {
            LogError(e);
        }
        finally
        {
            _dataLock.ExitReadLock();
            Interlocked.Exchange(ref _isSending, 0);
        }
    }

    private void LogError(Exception e)
    {
        if (_state.Value == TransponderState.ErrorToSend)
        {
            return;
        }

        _state.Value = TransponderState.ErrorToSend;
        _logger.ZLogError($"{_message.Name} sending error:{e.Message}");
    }

    private void LogSuccess()
    {
        if (_state.Value == TransponderState.Ok)
        {
            return;
        }

        _state.Value = TransponderState.Ok;
        _logger.ZLogDebug($"{_message.Name} start stream");
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
        using (_sync.EnterScope())
        {
            _timer?.Dispose();
            _timer = null;
            IsStarted = false;
        }
    }

    public void Set(Action<TMessage> changeCallback)
    {
        try
        {
            _dataLock.EnterWriteLock();
            changeCallback(_message);
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, $"Error to set new value for {_message.Name}:{e.Message}");
        }
        finally
        {
            _dataLock.ExitWriteLock();
        }
    }

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dataLock.Dispose();
            _state.Dispose();
            using (_sync.EnterScope())
            {
                _timer?.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await CastAndDispose(_dataLock).ConfigureAwait(false);
        await CastAndDispose(_state).ConfigureAwait(false);

        // ReSharper disable once InconsistentlySynchronizedField
        if (_timer != null)
        {
            await _timer.DisposeAsync().ConfigureAwait(false);
        }

        await base.DisposeAsyncCore().ConfigureAwait(false);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                resource.Dispose();
            }
        }
    }

    #endregion
}
