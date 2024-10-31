using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO
{
    public class TcpClientPort : PortBase
    {
        private readonly TcpPortConfig _cfg;
        private TcpClient _tcp;
        private CancellationTokenSource _stop;
        private DateTime _lastData;
        private static int _counter;

        public TcpClientPort(TcpPortConfig cfg)
        {
            _cfg = cfg;
        }

        public override PortType PortType { get; } = PortType.Tcp;

        public override string PortLogName => _cfg.ToString();

        protected override async Task InternalSend(
            ReadOnlyMemory<byte> data,
            CancellationToken cancel
        )
        {
            if (_tcp == null || _tcp.Connected == false)
            {
                return;
            }

            await _tcp.GetStream().WriteAsync(data, cancel);
        }

        protected override Task InternalSend(byte[] data, int count, CancellationToken cancel)
        {
            if (_tcp == null || _tcp.Connected == false)
            {
                return Task.CompletedTask;
            }

            return _tcp.GetStream().WriteAsync(data, 0, count, cancel);
        }

        protected override void InternalStop()
        {
            _tcp?.Close();
            _tcp?.Dispose();
            _tcp = null;
            _stop?.Cancel(false);
            _stop?.Dispose();
            _stop = null;
        }

        protected override void InternalStart()
        {
            _counter++;
            InternalStop();
            _tcp = new TcpClient();
            _tcp.Connect(_cfg.Host, _cfg.Port);
            _stop = new CancellationTokenSource();
            var recvThread = new Thread(ListenAsync)
            {
                Name = "TCP_C" + _counter,
                IsBackground = true,
                Priority = ThreadPriority.Normal,
            };
            _stop.Token.Register(() =>
            {
                try
                {
                    _tcp?.Close();
                    _tcp?.Dispose();
                    recvThread.Interrupt();
                }
                catch (Exception)
                {
                    Debug.Assert(false);
                }
            });
            recvThread.Start(_stop);
        }

        private async void ListenAsync(object obj)
        {
            var cancellationTokenSource = (CancellationTokenSource)obj;
            try
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    if (_cfg.ReconnectTimeout != 0)
                    {
                        if ((DateTime.Now - _lastData).TotalMilliseconds > _cfg.ReconnectTimeout)
                        {
                            await _tcp.GetStream()
                                .WriteAsync([], 0, 0, cancellationTokenSource.Token);
                        }
                    }

                    if (_tcp.Available != 0)
                    {
                        _lastData = DateTime.Now;
                        var buff = new byte[_tcp.Available];
                        var readed = await _tcp.GetStream()
                            .ReadAsync(buff, 0, buff.Length, cancellationTokenSource.Token);
                        Debug.Assert(readed == buff.Length);
                        if (readed != 0)
                        {
                            InternalOnData(buff);
                        }
                    }
                    else
                    {
                        await Task.Delay(30, cancellationTokenSource.Token).ConfigureAwait(false);
                    }
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
            catch (ThreadAbortException)
            {
                Debug.Assert(false);
            }
            catch (Exception e)
            {
                InternalOnError(e);
            }
        }

        protected override void InternalDisposeOnce()
        {
            base.InternalDisposeOnce();

            _tcp?.Dispose();
        }

        public override string ToString()
        {
            try
            {
                return $"TCP\\IP Client      {_tcp?.Client?.LocalEndPoint}:\n"
                    + $"Reconnect timeout   {_cfg.ReconnectTimeout} ms\n"
                    + $"Remote server       {_cfg.Host}:{_cfg.Port}";
            }
            catch (Exception)
            {
                return $"TCP\\IP Client      \n"
                    + $"Reconnect timeout   {_cfg.ReconnectTimeout} ms\n"
                    + $"Remote server       {_cfg.Host}:{_cfg.Port}";
            }
        }
    }
}
