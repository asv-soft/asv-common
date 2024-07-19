using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;

namespace Asv.IO
{
    public class TcpServerPort : PortBase
    {
        private readonly TcpPortConfig _cfg;
        private TcpListener _tcp;
        private CancellationTokenSource _stop;
        private readonly List<TcpClient> _clients = new();
        private readonly ReaderWriterLockSlim _rw = new();
        private readonly Subject<TcpClient> _removeTcpClientSubject;
        private readonly Subject<TcpClient> _addTcpClientSubject;

        public TcpServerPort(TcpPortConfig cfg)
        {
            _cfg = cfg;
            _removeTcpClientSubject = new Subject<TcpClient>().DisposeItWith(Disposable);
            _addTcpClientSubject = new Subject<TcpClient>().DisposeItWith(Disposable);
            Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)).Where(_=>IsEnabled.Value).Subscribe(DeleteClients, DisposeCancel);
            DisposeCancel.Register(InternalStop);
        }

        public IObservable<TcpClient> OnTcpClientRemoved => _removeTcpClientSubject;
        public IObservable<TcpClient> OnTcpClientAdded => _addTcpClientSubject;

        private void DeleteClients(long l)
        {
            _rw.EnterUpgradeableReadLock();
            try
            {

                var itemsToDelete = _clients.Where(_ => _.Connected == false).ToArray();
                if (itemsToDelete.Length == 0) return;
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
            var clients = _clients.ToArray();
            _rw.ExitReadLock();
            return Task.WhenAll(clients.Select(_ => SendAsync(_, data, cancel)));
        }

        

        protected override Task InternalSend(byte[] data, int count, CancellationToken cancel)
        {
            _rw.EnterReadLock();
            var clients = _clients.ToArray();
            _rw.ExitReadLock();
            return Task.WhenAll(clients.Select(_ => SendAsync(_, data, count, cancel)));
        }

        private async Task SendAsync(TcpClient client, byte[] data, int count, CancellationToken cancel)
        {
            if (_tcp == null || client == null || client.Connected == false) return;
            try
            {
                await client.GetStream().WriteAsync(data, 0, count, cancel).ConfigureAwait(false);
            }
            catch (Exception)
            {
               // Debug.Assert(false);    
            }
        }
        private async Task SendAsync(TcpClient client, ReadOnlyMemory<byte> data, CancellationToken cancel)
        {
            if (_tcp == null || client == null || client.Connected == false) return;
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
            _tcp = new TcpListener(IPAddress.Parse(_cfg.Host), _cfg.Port);
            _tcp.Start();
            _stop = new CancellationTokenSource();
            var recvConnectionThread = new Thread(RecvConnectionCallback) { IsBackground = true, Priority = ThreadPriority.Lowest };
            var recvDataThread = new Thread(RecvDataCallback) { IsBackground = true, Priority = ThreadPriority.Lowest };
            
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

        private void RecvConnectionCallback(object obj)
        {
            try
            {
                while (_stop?.IsCancellationRequested == false)
                {
                    try
                    {
                        var newClient = _tcp.AcceptTcpClient();
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
                        Debug.Assert(false);
                        // ignore
                    }

                }
            }
            catch (ThreadAbortException)
            {
                // ignore
            }

        }

        private void RecvDataCallback(object obj)
        {
            try
            {
                while (_stop?.IsCancellationRequested == false)
                {
                    _rw.EnterReadLock();
                    var clients = _clients.ToArray();
                    _rw.ExitReadLock();

                    foreach (var tcpClient in clients)
                    {
                        var data = RecvClientData(tcpClient);
                        if (data != null)
                        {
                            foreach (var otherClients in clients)
                            {
                                // send to all except the client from whom we received the data
                                if (otherClients == tcpClient) continue;
                                SendData(otherClients,data);
                            }
                            InternalOnData(data);
                        }
                        
                    }
                    Thread.Sleep(30);
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.Interrupted) return;
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

        protected override void InternalDisposeOnce()
        {
            base.InternalDisposeOnce();
            _tcp?.Stop();
        }

        private void SendData(TcpClient tcpClient, byte[] data)
        {
            if (tcpClient.Connected == false) return;
            if (tcpClient.Client.Connected == false) return;
            try
            {
                tcpClient.GetStream().Write(data, 0, data.Length);
            }
            catch (Exception)
            {
                // ignore
            }
        }

        private byte[] RecvClientData(TcpClient tcpClient)
        {
            if (tcpClient.Available == 0) return null;
            if (tcpClient.Connected == false) return null;
            if (tcpClient.Client.Connected == false) return null;
            var buff = new byte[tcpClient.Available];
            try
            {
                ``````-+------------+-+`` ```+`+```                        ..................................+ 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                + 
                tcpClient.GetStream().Read(buff, 0, buff.Length);
            }
            catch (ThreadAbortException)
            {
                // ignore
            }
            
            return buff;
        }

        

        public override string ToString()
        {
            var count = 0;
            string message;
            try
            {
                _rw.EnterReadLock();
                count = _clients.Count;
                message = string.Join("\n", _clients.Select(_ => $"   - {_.Client.RemoteEndPoint}"));
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

            
            return $"TCP\\IP Server      {_cfg.Host}:{_cfg.Port} \n" +
                   $"Reconnect timeout: {_cfg.ReconnectTimeout:N0} ms\n" +
                   $"Clients [{count}]:\n" +
                   $"{message}";
        }
    }
}
