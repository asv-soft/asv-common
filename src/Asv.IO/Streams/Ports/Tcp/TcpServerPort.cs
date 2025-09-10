using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using R3;

namespace Asv.IO
{
    public class TcpServerPort : PortBase
    {
        private readonly TcpPortConfig _cfg;
        private TcpListener? _tcp;
        private CancellationTokenSource? _stop;
        private readonly List<TcpClient> _clients = [];
        private readonly ReaderWriterLockSlim _rw = new();
        private readonly Subject<TcpClient> _removeTcpClientSubject;
        private readonly Subject<TcpClient> _addTcpClientSubject;
        private readonly ITimer _timer;

        public TcpServerPort(
            TcpPortConfig cfg,
            TimeProvider? timeProvider = null,
            ILogger? logger = null
        )
            : base(timeProvider, logger)
        {
            _cfg = cfg;
            _removeTcpClientSubject = new Subject<TcpClient>();
            _addTcpClientSubject = new Subject<TcpClient>();
            _timer = TimeProvider.CreateTimer(
                DeleteClients,
                null,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(3)
            );
            DisposeCancel.Register(InternalStop);
        }

        public Observable<TcpClient> OnTcpClientRemoved => _removeTcpClientSubject;
        public Observable<TcpClient> OnTcpClientAdded => _addTcpClientSubject;

        private void DeleteClients(object? o)
        {
            _rw.EnterUpgradeableReadLock();
            try
            {
                var itemsToDelete = _clients.Where(x => x.Connected == false).ToImmutableArray();
                if (itemsToDelete.Length == 0)
                {
                    return;
                }

                _rw.EnterWriteLock();
                try
                {
                    foreach (var tcpClient in itemsToDelete)
                    {
                        _clients.Remove(tcpClient);
                        _removeTcpClientSubject.OnNext(tcpClient);
                    }
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                InternalOnError(e);
                Debug.Assert(false);
            }
            finally
            {
                _rw.ExitUpgradeableReadLock();
            }
        }

        public override PortType PortType { get; } = PortType.Tcp;

        public override string PortLogName => _cfg.ToString();

        protected override Task InternalSend(ReadOnlyMemory<byte> data, CancellationToken cancel)
        {
            _rw.EnterReadLock();
            var clients = _clients.ToImmutableArray();
            _rw.ExitReadLock();
            return Task.WhenAll(clients.Select(x => SendAsync(x, data, cancel)));
        }

        protected override Task InternalSend(byte[] data, int count, CancellationToken cancel)
        {
            _rw.EnterReadLock();
            var clients = _clients.ToImmutableArray();
            _rw.ExitReadLock();
            return Task.WhenAll(clients.Select(x => SendAsync(x, data, count, cancel)));
        }

        private async Task SendAsync(
            TcpClient client,
            byte[] data,
            int count,
            CancellationToken cancel
        )
        {
            if (_tcp == null || client.Connected == false)
            {
                return;
            }

            try
            {
                await client.GetStream().WriteAsync(data, 0, count, cancel).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Debug.Assert(false);
            }
        }

        private async Task SendAsync(
            TcpClient client,
            ReadOnlyMemory<byte> data,
            CancellationToken cancel
        )
        {
            if (_tcp == null || client.Connected == false)
            {
                return;
            }

            try
            {
                await client.GetStream().WriteAsync(data, cancel).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Debug.Assert(false);
            }
        }

        protected override void InternalStop()
        {
            _stop?.Cancel(false);
            _stop?.Dispose();
            _stop = null;
        }

        protected override void InternalStart()
        {
            _tcp = new TcpListener(
                IPAddress.Parse(_cfg.Host ?? throw new InvalidOperationException()),
                _cfg.Port
            );
            _tcp.Start();
            _stop = new CancellationTokenSource();
            var recvConnectionThread = new Thread(RecvConnectionCallback) { IsBackground = true };
            var recvDataThread = new Thread(RecvDataCallback) { IsBackground = true };

            _stop.Token.Register(() =>
            {
                try
                {
                    _rw.EnterWriteLock();
                    foreach (var client in _clients.ToArray())
                    {
                        client.Close();
                    }

                    _clients.Clear();
                    _tcp.Stop();

                    // recvDataThread.Interrupt();
                    // recvConnectionThread.Interrupt();
                }
                catch (Exception)
                {
                    Debug.Assert(false);

                    // ignore
                }
                finally
                {
                    _rw.ExitWriteLock();
                }
            });
            recvDataThread.Start();
            recvConnectionThread.Start();
        }

        private void RecvConnectionCallback(object? obj)
        {
            try
            {
                while (_stop is { IsCancellationRequested: false })
                {
                    try
                    {
                        var newClient = _tcp?.AcceptTcpClient();
                        if (newClient == null)
                        {
                            continue;
                        }

                        _rw.EnterWriteLock();
                        _clients.Add(newClient);
                        _rw.ExitWriteLock();
                        _addTcpClientSubject.OnNext(newClient);
                    }
                    catch (ThreadAbortException)
                    {
                        // ignore
                    }
                    catch (SocketException)
                    {
                        // ignore
                    }
                    catch (Exception)
                    {
                        // ignore
                        Debug.Assert(false);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                // ignore
            }
        }

        private async void RecvDataCallback(object? obj)
        {
            try
            {
                while (_stop is { IsCancellationRequested: false })
                {
                    var someDataReceived = false;
                    _rw.EnterReadLock();
                    var clients = _clients.ToImmutableArray();
                    _rw.ExitReadLock();
                    foreach (var tcpClient in clients)
                    {
                        if (tcpClient.Available == 0)
                        {
                            continue;
                        }

                        if (tcpClient.Connected == false)
                        {
                            continue;
                        }

                        if (tcpClient.Client.Connected == false)
                        {
                            continue;
                        }

                        someDataReceived = true;
                        var buff = ArrayPool<byte>.Shared.Rent(tcpClient.Available);
                        try
                        {
                            var read = tcpClient.GetStream().Read(buff, 0, buff.Length);
                            foreach (var otherClients in clients)
                            {
                                // send to all except the client from whom we received the data
                                if (otherClients == tcpClient)
                                {
                                    continue;
                                }

                                if (otherClients.Connected == false)
                                {
                                    continue;
                                }

                                if (otherClients.Client.Connected == false)
                                {
                                    continue;
                                }

                                otherClients.GetStream().Write(buff, 0, read);
                            }
                            var buffer = new byte[read];
                            Buffer.BlockCopy(buff, 0, buffer, 0, read);
                            InternalOnData(buffer);
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(buff);
                        }
                    }

                    if (someDataReceived == false)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(30), TimeProvider);
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
                // ignore
            }
            catch (Exception e)
            {
                InternalOnError(e);
            }
        }

        public override string ToString()
        {
            var count = 0;
            string message;
            try
            {
                _rw.EnterReadLock();
                count = _clients.Count;
                message = string.Join(
                    "\n",
                    _clients.Select(_ => $"   - {_.Client.RemoteEndPoint}")
                );
            }
            catch (Exception e)
            {
                message = e.ToString();
                Debug.Assert(false);
            }
            finally
            {
                _rw.ExitReadLock();
            }

            return $"TCP\\IP Server      {_cfg.Host}:{_cfg.Port} \n"
                + $"Reconnect timeout: {_cfg.ReconnectTimeout:N0} ms\n"
                + $"Clients [{count}]:\n"
                + $"{message}";
        }

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tcp?.Dispose();
                _stop?.Cancel(false);
                _stop?.Dispose();
                _stop = null;

                _rw.Dispose();
                _removeTcpClientSubject.Dispose();
                _addTcpClientSubject.Dispose();
                _timer.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            if (_tcp != null)
            {
                await CastAndDispose(_tcp);
            }

            if (_stop != null)
            {
                _stop.Cancel(false);
                await CastAndDispose(_stop);
                _stop = null;
            }
            await CastAndDispose(_rw);
            await CastAndDispose(_removeTcpClientSubject);
            await CastAndDispose(_addTcpClientSubject);
            await _timer.DisposeAsync();

            await base.DisposeAsyncCore();

            return;

            static async ValueTask CastAndDispose(IDisposable resource)
            {
                if (resource is IAsyncDisposable resourceAsyncDisposable)
                {
                    await resourceAsyncDisposable.DisposeAsync();
                }
                else
                {
                    resource.Dispose();
                }
            }
        }

        #endregion
    }
}
