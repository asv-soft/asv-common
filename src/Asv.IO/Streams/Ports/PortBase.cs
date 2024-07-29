using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;

namespace Asv.IO
{
    public abstract class PortBase : DisposableOnceWithCancel, IPort
    {
        private int _isEvaluating;
        private readonly RxValue<Exception> _portErrorStream = new();
        private readonly RxValue<PortState> _portStateStream = new();
        private readonly RxValue<bool> _enableStream = new();
        private readonly Subject<byte[]> _outputData = new();
        private long _rxBytes;
        private long _txBytes;

        public long RxBytes => Interlocked.Read(ref _rxBytes);
        public long TxBytes => Interlocked.Read(ref _txBytes);
        public abstract PortType PortType { get; }
        public TimeSpan ReconnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public IRxValue<bool> IsEnabled => _enableStream;
        public IRxValue<PortState> State => _portStateStream;

        protected PortBase()
        {
            Disposable.AddAction(Disable);
            Disposable.AddAction(() =>
            {
                if (_outputData.IsDisposed == false)
                {
                    _outputData.OnCompleted();
                }
                _outputData.Dispose();
            });
            _enableStream.Where(x => x).Subscribe(_ => Task.Factory.StartNew(TryConnect)).DisposeItWith(Disposable);
            _portErrorStream.DisposeItWith(Disposable);
            _portStateStream.DisposeItWith(Disposable);
            _enableStream.DisposeItWith(Disposable);
            
            
        }

        public abstract string PortLogName { get; }

        public string Name => PortLogName;

        public async Task<bool> Send(byte[] data, int count, CancellationToken cancel)
        {
            if (!IsEnabled.Value) return false;
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
            if (!IsEnabled.Value) return false;
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

        public IRxValue<Exception> Error => _portErrorStream;

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
            if (Interlocked.CompareExchange(ref _isEvaluating, 1, 0) != 0) return;
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
            Observable.Timer(ReconnectTimeout).Subscribe(_ => TryConnect(),DisposeCancel);
            Stop();
        }
        
        public IDisposable Subscribe(IObserver<byte[]> observer)
        {
            return _outputData.Subscribe(observer);
        }
    }
}
