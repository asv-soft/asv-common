using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO
{
    public class PortWrapper : IDisposable
    {
        private readonly Action<PortWrapper, byte[], CancellationToken> _onRecv;
        private readonly CancellationTokenSource _cancel = new();

        public PortWrapper(
            IPort port,
            string id,
            PortSettings settings,
            Action<PortWrapper, byte[], CancellationToken> onRecv
        )
        {
            _onRecv = onRecv;
            Port = port;
            Id = id;
            Settings = settings;
            port.Subscribe(OnNext, _cancel.Token);
        }

        private void OnNext(byte[] bytes)
        {
            _onRecv(this, bytes, _cancel.Token);
        }

        public IPort Port { get; }
        public string Id { get; }

        public PortSettings Settings { get; }

        public void Dispose()
        {
            _cancel.Cancel(false);
            _cancel.Dispose();
            Port.Dispose();
        }
    }

    public class PortInfo : IPortInfo
    {
        public string Id { get; }
        public PortSettings Settings { get; }
        public string Description { get; set; }
        public string Status { get; }
        public long RxAcc { get; }
        public long TxAcc { get; }
        public PortState State { get; set; }
        public PortType Type { get; set; }
        public Exception LastException { get; set; }

        public PortInfo(PortWrapper wraper)
        {
            Id = wraper.Id;
            Settings = wraper.Settings;
            RxAcc = wraper.Port.RxBytes;
            TxAcc = wraper.Port.TxBytes;
            LastException = wraper.Port.Error.Value;
            Type = wraper.Port.PortType;
            State = wraper.Port.State.Value;
            Description = wraper.Port.ToString();
        }
    }

    public class PortManager : IPortManager
    {
        private readonly object _sync = new();
        private readonly List<PortWrapper> _ports = new();
        private readonly Subject<Unit> _configChangedSubject = new();
        private readonly Subject<byte[]> _onRecv = new();
        private long _rxBytes;
        private long _txBytes;

        public PortManager() { }

        public IPortInfo[] Ports => GetPortsInfo();

        private IPortInfo[] GetPortsInfo()
        {
            lock (_sync)
            {
                return _ports.Select(_ => new PortInfo(_)).Cast<IPortInfo>().ToArray();
            }
        }

        public void Add(PortSettings settings)
        {
            lock (_sync)
            {
                var port = PortFactory.Create(settings.ConnectionString);
                if (settings.IsEnabled)
                {
                    port.Enable();
                }
                else
                {
                    port.Disable();
                }

                var wrapper = new PortWrapper(port, Guid.NewGuid().ToString(), settings, OnRecv);
                _ports.Add(wrapper);
            }

            _configChangedSubject.OnNext(Unit.Default);
        }

        private void OnRecv(PortWrapper sender, byte[] data, CancellationToken cancel)
        {
            Interlocked.Add(ref _rxBytes, data.Length);
            IEnumerable<PortWrapper> ports;
            lock (_sync)
            {
                // repeat
                ports = _ports
                    .Where(_ => _.Id != sender.Id) // exclude self
                    .Where(_ => _.Port.IsEnabled.Value) // exclude disabled
                    .Where(_ => _.Port.State.Value == PortState.Connected); // only connected
            }

            _onRecv.OnNext(data);
            Task.WaitAll(
                ports.Select(_ => _.Port.Send(data, data.Length, cancel)).ToArray(),
                cancel
            );
        }

        public PortManagerSettings Save()
        {
            lock (_sync)
            {
                return new PortManagerSettings { Ports = _ports.Select(_ => _.Settings).ToArray() };
            }
        }

        public void Enable(string portId)
        {
            lock (_sync)
            {
                var item = _ports.FirstOrDefault(_ => _.Id == portId);
                if (item == null)
                {
                    return;
                }

                item.Port.Enable();
                item.Settings.IsEnabled = true;
            }

            _configChangedSubject.OnNext(Unit.Default);
        }

        public void Disable(string portId)
        {
            lock (_sync)
            {
                var item = _ports.FirstOrDefault(_ => _.Id == portId);
                if (item == null)
                {
                    return;
                }

                item.Port.Disable();
                item.Settings.IsEnabled = false;
            }

            _configChangedSubject.OnNext(Unit.Default);
        }

        public void Load(PortManagerSettings settings)
        {
            lock (_sync)
            {
                foreach (var port in settings.Ports)
                {
                    Add(port);
                }
            }

            _configChangedSubject.OnNext(Unit.Default);
        }

        public bool Remove(string portId)
        {
            lock (_sync)
            {
                var item = _ports.Find(_ => _.Id == portId);
                if (item == null)
                {
                    return false;
                }

                item.Dispose();
                _ports.Remove(item);
            }

            _configChangedSubject.OnNext(Unit.Default);
            return true;
        }

        public IObservable<Unit> OnConfigChanged => _configChangedSubject;

        public void Dispose()
        {
            PortWrapper[] ports;
            lock (_sync)
            {
                ports = _ports.Where(_ => _.Port.IsEnabled.Value).ToArray();
                _ports.Clear();
            }

            foreach (var port in ports)
            {
                port.Dispose();
            }

            _configChangedSubject?.OnCompleted();
            _configChangedSubject?.Dispose();
            _onRecv?.OnCompleted();
            _onRecv?.Dispose();
        }

        public IDisposable Subscribe(IObserver<byte[]> observer)
        {
            return _onRecv.Subscribe(observer);
        }

        public string Name => "PortManager";

        public async Task<bool> Send(byte[] data, int count, CancellationToken cancel)
        {
            Interlocked.Add(ref _txBytes, count);
            PortWrapper[] ports;
            lock (_sync)
            {
                ports = _ports.Where(_ => _.Port.IsEnabled.Value).ToArray();
            }

            var res = await Task.WhenAll(ports.Select(_ => _.Port.Send(data, count, cancel)))
                .ConfigureAwait(false);
            return res.Any();
        }

        public async Task<bool> Send(ReadOnlyMemory<byte> data, CancellationToken cancel)
        {
            Interlocked.Add(ref _txBytes, data.Length);
            PortWrapper[] ports;
            lock (_sync)
            {
                ports = _ports.Where(_ => _.Port.IsEnabled.Value).ToArray();
            }

            var res = await Task.WhenAll(ports.Select(_ => _.Port.Send(data, cancel)))
                .ConfigureAwait(false);
            return res.Any();
        }

        public long RxBytes => Interlocked.Read(ref _rxBytes);
        public long TxBytes => Interlocked.Read(ref _txBytes);
    }
}
