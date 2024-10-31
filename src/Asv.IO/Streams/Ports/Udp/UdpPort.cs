using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;

namespace Asv.IO
{
    public class UdpPort : PortBase
    {
        private readonly UdpPortConfig _config;
        private readonly IPEndPoint _recvEndPoint;
        private UdpClient _udp;
        private IPEndPoint _lastRecvEndpoint;
        private CancellationTokenSource _stop;
        private readonly IPEndPoint _sendEndPoint;
        private readonly Subject<IPEndPoint> _onRecvNewClientSubject;

        public UdpPort(UdpPortConfig config)
        {
            _config = config;
            _recvEndPoint = new IPEndPoint(IPAddress.Parse(config.LocalHost), config.LocalPort);
            if (!string.IsNullOrWhiteSpace(config.RemoteHost) && config.RemotePort != 0)
            {
                _sendEndPoint = new IPEndPoint(
                    IPAddress.Parse(config.RemoteHost),
                    config.RemotePort
                );
            }

            _onRecvNewClientSubject = new Subject<IPEndPoint>().DisposeItWith(Disposable);
        }

        public IObservable<IPEndPoint> OnRecvNewClient => _onRecvNewClientSubject;

        public override PortType PortType => PortType.Udp;

        public override string PortLogName => _config.ToString();

        protected override async Task InternalSend(
            ReadOnlyMemory<byte> data,
            CancellationToken cancel
        )
        {
            if (_udp?.Client == null || _udp.Client.Connected == false)
            {
                return;
            }

            await _udp.SendAsync(data, cancel);
        }

        protected override Task InternalSend(byte[] data, int count, CancellationToken cancel)
        {
            if (_udp?.Client == null || _udp.Client.Connected == false)
            {
                return Task.CompletedTask;
            }

            return _udp.SendAsync(data, count);
        }

        protected override void InternalStop()
        {
            _stop?.Cancel(false);
            _udp?.Dispose();
        }

        protected override void InternalStart()
        {
            _udp = new UdpClient(_recvEndPoint);
            if (_sendEndPoint != null)
            {
                _udp.Connect(_sendEndPoint);
            }

            _stop = new CancellationTokenSource();
            var recvThread = new Thread(() => ListenAsync(_stop.Token))
            {
                IsBackground = true,
                Priority = ThreadPriority.Lowest,
            };
            _stop.Token.Register(() =>
            {
                try
                {
                    _stop.Cancel();
                }
                catch
                {
                    // ignore
                }
            });
            recvThread.Start();
        }

        private void ListenAsync(object obj)
        {
            try
            {
                var anyEp = new IPEndPoint(IPAddress.Any, _recvEndPoint.Port);
                while (true)
                {
                    var bytes = _udp.Receive(ref anyEp);
                    if (_lastRecvEndpoint == null && _udp.Client.Connected == false)
                    {
                        _lastRecvEndpoint = anyEp;
                        _udp.Connect(_lastRecvEndpoint);
                        _onRecvNewClientSubject.OnNext(_lastRecvEndpoint);
                    }

                    InternalOnData(bytes);
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.Interrupted)
                {
                    return;
                }

                InternalOnError(ex);
            }
            catch (Exception e)
            {
                InternalOnError(e);
            }
        }

        protected override void InternalDisposeOnce()
        {
            base.InternalDisposeOnce();
            _udp?.Dispose();
        }

        public override string ToString()
        {
            return $"UDP {_config.LocalHost}:{_config.LocalPort}\n Remote IP:{_config.RemoteHost}:{_config.RemotePort}";
        }
    }
}
