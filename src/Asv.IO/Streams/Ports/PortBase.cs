using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using R3;
using ZLogger;
using ObservableExtensions = System.ObservableExtensions;

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
        private volatile int _isDisposed;

        protected PortBase(TimeProvider? timeProvider = null, ILogger? logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
            _timeProvider = timeProvider ?? TimeProvider.System; 
            _sub1 = _enableStream.Where(x => x).Subscribe(TryConnect, (_,action) => Task.Factory.StartNew(action));
        }

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
            if (!IsEnabled.CurrentValue) return false;
            if (_portStateStream.Value != PortState.Connected) return false;
            try
            {
                await InternalSend(data, count, cancel).ConfigureAwait(false);
                Interlocked.Add(ref _txBytes, count);
                return true;
            }
            catch (Exception exception)
            {
                InternalOnError(exception);
                return false;
            }
        }
        
        public async Task<bool> Send(ReadOnlyMemory<byte> data, CancellationToken cancel)
        {
            if (!IsEnabled.CurrentValue) return false;
            if (_portStateStream.Value != PortState.Connected) return false;
            try
            {
                await InternalSend(data, cancel).ConfigureAwait(false);
                Interlocked.Add(ref _txBytes, data.Length);
                return true;
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
            _enableStream.OnNext(true);
        }

        public void Disable()
        {
            _enableStream.OnNext(false);
            _portStateStream.OnNext(PortState.Disabled);
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
                Debug.Assert(true,ex.Message);
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
                if (!_enableStream.Value) return;
                if (IsDisposed) return;
                _portStateStream.OnNext(PortState.Connecting);
                InternalStart();
                _portStateStream.OnNext(PortState.Connected);
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
            catch (Exception)
            {
                // ignored
            }
        }

        protected void InternalOnError(Exception exception)
        {
            
            _portStateStream.OnNext(PortState.Error);
            _portErrorStream.OnNext(exception);
            _reconnectTimer = _timeProvider.CreateTimer(x => TryConnect(), null, ReconnectTimeout, Timeout.InfiniteTimeSpan);
            Stop();
        }

        protected CancellationToken DisposeCancel => _disposeCancel.Token;
        protected bool IsDisposed => _isDisposed != 0;

        #region Dispose

        protected virtual void Dispose(bool disposing)
        {
            
            if (disposing)
            {
                _portErrorStream.Dispose();
                _portStateStream.Dispose();
                _enableStream.Dispose();
                _outputData.Dispose();
                _sub1.Dispose();
                _disposeCancel.Dispose();
                _reconnectTimer?.Dispose();
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            await CastAndDispose(_portErrorStream);
            await CastAndDispose(_portStateStream);
            await CastAndDispose(_enableStream);
            await CastAndDispose(_outputData);
            await CastAndDispose(_sub1);
            await CastAndDispose(_disposeCancel);
            if (_reconnectTimer != null) await _reconnectTimer.DisposeAsync();

            return;

            static async ValueTask CastAndDispose(IDisposable resource)
            {
                if (resource is IAsyncDisposable resourceAsyncDisposable)
                    await resourceAsyncDisposable.DisposeAsync();
                else
                    resource.Dispose();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;
            await DisposeAsyncCore();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
