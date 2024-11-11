using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using R3;

namespace Asv.IO
{
    public class UdpPort : PortBase
    {
        private readonly UdpPortConfig _config;
        private readonly IPEndPoint _receiveEndPoint;
        private UdpClient? _udp;
        private IPEndPoint? _lastReceiveEndpoint;
        private CancellationTokenSource? _stop;
        private readonly IPEndPoint? _sendEndPoint;
        private readonly Subject<IPEndPoint> _onReceiveNewClientSubject;
        private Thread? _receiveThread;

        public UdpPort(UdpPortConfig config, TimeProvider? timeProvider = null, ILogger? logger = null)
            : base(timeProvider, logger)
        {
            _config = config;
            _receiveEndPoint = new IPEndPoint(IPAddress.Parse(config.LocalHost), config.LocalPort);
            if (!string.IsNullOrWhiteSpace(config.RemoteHost) && config.RemotePort != 0)
            {
                _sendEndPoint = new IPEndPoint(IPAddress.Parse(config.RemoteHost), config.RemotePort);
            }

            _onReceiveNewClientSubject = new Subject<IPEndPoint>();
        }

        public Observable<IPEndPoint> OnReceiveNewClient => _onReceiveNewClientSubject;

        public override PortType PortType => PortType.Udp;

        public override string PortLogName => _config.ToString();
        protected override async Task InternalSend(ReadOnlyMemory<byte> data, CancellationToken cancel)
        {
            if (_udp?.Client == null || _udp.Client.Connected == false) return;
            await _udp.SendAsync(data, cancel);
        }

        protected override Task InternalSend(byte[] data, int count, CancellationToken cancel)
        {
            if (_udp?.Client == null || _udp.Client.Connected == false) return Task.CompletedTask;
            return _udp.SendAsync(data, count);
        }

        protected override void InternalStop()
        {
            _stop?.Cancel(false);
            _udp?.Dispose();
        }

        protected override void InternalStart()
        {
            _udp = new UdpClient(_receiveEndPoint);
            if (_sendEndPoint != null)
            {
                _udp.Connect(_sendEndPoint);
            }
            _stop = new CancellationTokenSource();
            _receiveThread = new Thread(ListenAsync) { IsBackground = true };
            _receiveThread.Start();
            
        }

        private void ListenAsync(object? obj)
        {
            try
            {
                var anyEp = new IPEndPoint(IPAddress.Any, _receiveEndPoint.Port);
                while (_stop != null || _stop?.IsCancellationRequested == false)
                {
                    var udp = _udp;
                    if (udp == null) break;
                    var bytes = udp.Receive(ref anyEp);
                    if (_lastReceiveEndpoint == null && udp.Client.Connected == false)
                    {
                        _lastReceiveEndpoint = anyEp;
                        udp.Connect(_lastReceiveEndpoint);
                        _onReceiveNewClientSubject.OnNext(_lastReceiveEndpoint);
                    }
                    InternalOnData(bytes);
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.Interrupted) return;
                InternalOnError(ex);
            }
            catch (Exception e)
            {
                InternalOnError(e);
            }
        }

        public override string ToString()
        {
            return $"UDP {_config.LocalHost}:{_config.LocalPort}\n Remote IP:{_config.RemoteHost}:{_config.RemotePort}";
        }
        
        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _udp?.Dispose();
                _udp = null;
                _stop?.Cancel(false);
                _stop?.Dispose();
                _stop = null;
                _onReceiveNewClientSubject.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            if (_udp != null)
            {
                await CastAndDispose(_udp);
                _udp = null;
            }

            if (_stop != null)
            {
                _stop.Cancel(false);
                await CastAndDispose(_stop);
                _stop = null;
            }
            await CastAndDispose(_onReceiveNewClientSubject);

            await base.DisposeAsyncCore();

            return;

            static async ValueTask CastAndDispose(IDisposable resource)
            {
                if (resource is IAsyncDisposable resourceAsyncDisposable)
                    await resourceAsyncDisposable.DisposeAsync();
                else
                    resource.Dispose();
            }
        }

        #endregion
    }
}
