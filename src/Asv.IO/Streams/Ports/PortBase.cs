using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using R3;
using ZLogger;

namespace Asv.IO
{
    public abstract class PortBase : IPort, IDisposable, IAsyncDisposable
    {
        private readonly ILogger _logger;
        private int _isEvaluating;
        private readonly ReactiveProperty<Exception?> _portErrorStream = new();
        private readonly ReactiveProperty<PortState> _portStateStream = new(PortState.Disabled);
        private readonly ReactiveProperty<bool> _enableStream = new(false);
        private readonly Subject<byte[]> _outputData = new();
        private long _rxBytes;
        private long _txBytes;
        private readonly TimeProvider _timeProvider;
        private readonly IDisposable _sub1;
        private readonly CancellationTokenSource _disposeCancel = new();
        private ITimer? _reconnectTimer;
        private readonly Lock _reconnectSync = new();
        private long _reconnectGeneration;
        private volatile int _isDisposed;

        protected PortBase(TimeProvider? timeProvider, ILogger? logger)
        {
            _logger = logger ?? NullLogger.Instance;
            _timeProvider = timeProvider ?? TimeProvider.System;
            _sub1 = _enableStream
                .Where(x => x)
                .Subscribe(TryConnect, (_, action) => Task.Factory.StartNew(action));
        }

        protected TimeProvider TimeProvider => _timeProvider;
        public long RxBytes => Interlocked.Read(ref _rxBytes);
        public long TxBytes => Interlocked.Read(ref _txBytes);
        public abstract PortType PortType { get; }
        public TimeSpan ReconnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public Observable<byte[]> OnReceive => _outputData;
        public ReadOnlyReactiveProperty<bool> IsEnabled => _enableStream;
        public ReadOnlyReactiveProperty<PortState> State => _portStateStream;
        public ReadOnlyReactiveProperty<Exception?> Error => _portErrorStream;
        public abstract string PortLogName { get; }
        public string Name => PortLogName;

        public async Task<bool> Send(byte[] data, int count, CancellationToken cancel)
        {
            if (!IsEnabled.CurrentValue)
            {
                return false;
            }

            if (_portStateStream.Value != PortState.Connected)
            {
                return false;
            }

            try
            {
                await InternalSend(data, count, cancel).ConfigureAwait(false);
                Interlocked.Add(ref _txBytes, count);
                return true;
            }
            catch (OperationCanceledException) when (cancel.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception exception)
            {
                InternalOnError(exception);
                return false;
            }
        }

        public async Task<bool> Send(ReadOnlyMemory<byte> data, CancellationToken cancel)
        {
            if (!IsEnabled.CurrentValue)
            {
                return false;
            }

            if (_portStateStream.Value != PortState.Connected)
            {
                return false;
            }

            try
            {
                await InternalSend(data, cancel).ConfigureAwait(false);
                Interlocked.Add(ref _txBytes, data.Length);
                return true;
            }
            catch (OperationCanceledException) when (cancel.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception exception)
            {
                InternalOnError(exception);
                return false;
            }
        }

        protected abstract Task InternalSend(ReadOnlyMemory<byte> data, CancellationToken cancel);

        public void Enable()
        {
            _enableStream.Value = true;
        }

        public void Disable()
        {
            _enableStream.Value = false;
            CancelReconnect();
            _portStateStream.Value = PortState.Disabled;
            Task.Factory.StartNew(Stop, DisposeCancel);
        }

        private void Stop()
        {
            try
            {
                InternalStop();
            }
            catch (Exception ex)
            {
                Debug.Assert(true, ex.Message);
            }
        }

        private void TryConnect()
        {
            if (Interlocked.CompareExchange(ref _isEvaluating, 1, 0) != 0)
            {
                _logger.ZLogTrace($"Duplicate call TryConnect() for port {PortLogName}");
                return;
            }
            try
            {
                if (IsDisposed || !_enableStream.Value)
                {
                    return;
                }

                if (_portStateStream.Value == PortState.Connected)
                {
                    return;
                }

                _portStateStream.Value = PortState.Connecting;
                InternalStart();
                _portStateStream.Value = PortState.Connected;
            }
            catch (Exception e)
            {
                InternalOnError(e);
            }
            finally
            {
                Interlocked.Exchange(ref _isEvaluating, 0);
            }
        }

        protected abstract Task InternalSend(byte[] data, int count, CancellationToken cancel);

        protected abstract void InternalStop();

        protected abstract void InternalStart();

        protected void InternalOnData(byte[] data)
        {
            try
            {
                Interlocked.Add(ref _rxBytes, data.Length);
                _outputData.OnNext(data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        protected void InternalOnError(Exception exception)
        {
            if (IsDisposed || !_enableStream.Value)
            {
                return;
            }
            _portStateStream.Value = PortState.Error;
            _portErrorStream.OnNext(exception);
            Stop();
            using (_reconnectSync.EnterScope())
            {
                if (IsDisposed || !_enableStream.Value)
                {
                    return;
                }
                var generation = ++_reconnectGeneration;
                _reconnectTimer?.Dispose();
                _reconnectTimer = _timeProvider.CreateTimer(
                    Reconnect,
                    generation,
                    ReconnectTimeout,
                    Timeout.InfiniteTimeSpan
                );
            }
        }

        private void Reconnect(object? state)
        {
            using (_reconnectSync.EnterScope())
            {
                if (
                    IsDisposed
                    || state is not long generation
                    || generation != _reconnectGeneration
                )
                {
                    return;
                }
                _reconnectTimer?.Dispose();
                _reconnectTimer = null;
            }
            TryConnect();
        }

        private void CancelReconnect()
        {
            using (_reconnectSync.EnterScope())
            {
                ++_reconnectGeneration;
                _reconnectTimer?.Dispose();
                _reconnectTimer = null;
            }
        }

        protected CancellationToken DisposeCancel => _disposeCancel.Token;
        protected bool IsDisposed => _isDisposed != 0;

        #region Dispose

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                CancelReconnect();
                _portErrorStream.Dispose();
                _portStateStream.Dispose();
                _enableStream.Dispose();
                _outputData.Dispose();
                _sub1.Dispose();
                _disposeCancel.Dispose();
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            {
                return;
            }

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            CancelReconnect();
            await CastAndDispose(_portErrorStream).ConfigureAwait(false);
            await CastAndDispose(_portStateStream).ConfigureAwait(false);
            await CastAndDispose(_enableStream).ConfigureAwait(false);
            await CastAndDispose(_outputData).ConfigureAwait(false);
            await CastAndDispose(_sub1).ConfigureAwait(false);
            await CastAndDispose(_disposeCancel).ConfigureAwait(false);

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

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            {
                return;
            }

            await DisposeAsyncCore().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
