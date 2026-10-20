using System;
using System.Buffers;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using R3;

namespace Asv.IO;

public class TcpServerPort : PortBase
{
    private readonly TcpPortConfig _cfg;
    private readonly Lock _clientsSync = new();
    private ImmutableArray<ClientConnection> _clients = [];
    private readonly Subject<TcpClient> _removeTcpClientSubject = new();
    private readonly Subject<TcpClient> _addTcpClientSubject = new();
    private TcpListener? _tcp;
    private CancellationTokenSource? _stop;
    private Task _acceptTask = Task.CompletedTask;
    private Task _readTask = Task.CompletedTask;
    private Task _shutdownTask = Task.CompletedTask;

    public TcpServerPort(
        TcpPortConfig cfg,
        TimeProvider? timeProvider = null,
        ILogger? logger = null
    )
        : base(timeProvider, logger)
    {
        _cfg = cfg;
    }

    public Observable<TcpClient> OnTcpClientRemoved => _removeTcpClientSubject;
    public Observable<TcpClient> OnTcpClientAdded => _addTcpClientSubject;
    public override PortType PortType => PortType.Tcp;
    public override string PortLogName => _cfg.ToString();
    internal EndPoint? LocalEndPoint => _tcp?.LocalEndpoint;

    private ImmutableArray<ClientConnection> GetClients()
    {
        using (_clientsSync.EnterScope())
        {
            return _clients;
        }
    }

    protected override Task InternalSend(ReadOnlyMemory<byte> data, CancellationToken cancel)
    {
        return Task.WhenAll(GetClients().Select(client => client.Writer.SendAsync(data, cancel)));
    }

    protected override Task InternalSend(byte[] data, int count, CancellationToken cancel)
    {
        return InternalSend(data.AsMemory(0, count), cancel);
    }

    protected override void InternalStop()
    {
        ImmutableArray<ClientConnection> clients;
        CancellationTokenSource? stop;
        TcpListener? listener;
        Task acceptTask;
        Task readTask;
        using (_clientsSync.EnterScope())
        {
            clients = _clients;
            _clients = [];
            stop = _stop;
            _stop = null;
            listener = _tcp;
            _tcp = null;
            acceptTask = _acceptTask;
            readTask = _readTask;
        }
        stop?.Cancel();
        listener?.Stop();
        foreach (var client in clients)
        {
            client.Dispose();
        }
        stop?.Dispose();
        _shutdownTask = Task.WhenAll(
            _shutdownTask,
            acceptTask,
            readTask,
            Task.WhenAll(clients.Select(client => client.Writer.Completion))
        );
    }

    protected override void InternalStart()
    {
        InternalStop();
        var listener = new TcpListener(
            IPAddress.Parse(_cfg.Host ?? throw new InvalidOperationException()),
            _cfg.Port
        );
        try
        {
            listener.Start();
            using (_clientsSync.EnterScope())
            {
                if (IsDisposed || !IsEnabled.CurrentValue)
                {
                    listener.Stop();
                    return;
                }
                _tcp = listener;
                _stop = new CancellationTokenSource();
                var cancel = _stop.Token;
                _acceptTask = AcceptClientsAsync(listener, cancel);
                _readTask = ReceiveDataAsync(cancel);
            }
        }
        catch
        {
            listener.Stop();
            throw;
        }
    }

    private async Task AcceptClientsAsync(TcpListener listener, CancellationToken cancel)
    {
        try
        {
            while (!cancel.IsCancellationRequested)
            {
                var socket = await listener.AcceptTcpClientAsync(cancel).ConfigureAwait(false);
                var client = new ClientConnection(socket, RemoveClient);
                using (_clientsSync.EnterScope())
                {
                    if (cancel.IsCancellationRequested)
                    {
                        client.Dispose();
                        return;
                    }
                    _clients = _clients.Add(client);
                }
                _addTcpClientSubject.OnNext(socket);
            }
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (cancel.IsCancellationRequested) { }
        catch (SocketException) when (cancel.IsCancellationRequested) { }
        catch (Exception ex)
        {
            InternalOnError(ex);
        }
    }

    private async Task ReceiveDataAsync(CancellationToken cancel)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            while (!cancel.IsCancellationRequested)
            {
                var received = false;
                foreach (var client in GetClients())
                {
                    try
                    {
                        var available = client.Socket.Available;
                        if (available == 0)
                        {
                            if (
                                client.Socket.Poll(0, SelectMode.SelectRead)
                                && client.Socket.Available == 0
                            )
                            {
                                RemoveClient(client);
                            }
                            continue;
                        }
                        var read = await client
                            .Client.GetStream()
                            .ReadAsync(
                                buffer.AsMemory(0, Math.Min(available, buffer.Length)),
                                cancel
                            )
                            .ConfigureAwait(false);
                        if (read == 0)
                        {
                            RemoveClient(client);
                            continue;
                        }
                        received = true;
                        foreach (var other in GetClients())
                        {
                            if (other != client && !other.Writer.TrySend(buffer.AsSpan(0, read)))
                            {
                                RemoveClient(other);
                            }
                        }
                        InternalOnData(buffer.AsSpan(0, read).ToArray());
                    }
                    catch (OperationCanceledException) when (cancel.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (IOException)
                    {
                        RemoveClient(client);
                    }
                    catch (SocketException)
                    {
                        RemoveClient(client);
                    }
                    catch (ObjectDisposedException)
                    {
                        RemoveClient(client);
                    }
                }
                if (!received)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(30), TimeProvider, cancel)
                        .ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested) { }
        catch (Exception ex)
        {
            InternalOnError(ex);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private void RemoveClient(ClientConnection client)
    {
        using (_clientsSync.EnterScope())
        {
            if (!_clients.Contains(client))
            {
                return;
            }
            _clients = _clients.Remove(client);
        }
        client.Dispose();
        if (!IsDisposed)
        {
            _removeTcpClientSubject.OnNext(client.Client);
        }
    }

    public override string ToString()
    {
        var clients = GetClients();
        return $"TCP\\IP Server      {_cfg.Host}:{_cfg.Port} \n"
            + $"Reconnect timeout: {_cfg.ReconnectTimeout:N0} ms\n"
            + $"Clients [{clients.Length}]:\n"
            + string.Join("\n", clients.Select(client => $"   - {client.RemoteEndPoint}"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            InternalStop();
            _removeTcpClientSubject.Dispose();
            _addTcpClientSubject.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        InternalStop();
        await _shutdownTask.ConfigureAwait(false);
        _removeTcpClientSubject.Dispose();
        _addTcpClientSubject.Dispose();
        await base.DisposeAsyncCore().ConfigureAwait(false);
    }

    private sealed class ClientConnection : IDisposable
    {
        public ClientConnection(TcpClient client, Action<ClientConnection> remove)
        {
            Client = client;
            Socket = client.Client;
            RemoteEndPoint = Socket.RemoteEndPoint;
            Writer = new TcpClientSendQueue(client.GetStream(), () => remove(this));
        }

        public TcpClient Client { get; }
        public Socket Socket { get; }
        public EndPoint? RemoteEndPoint { get; }
        public TcpClientSendQueue Writer { get; }

        public void Dispose()
        {
            Writer.Dispose();
            Client.Dispose();
        }
    }
}

internal sealed class TcpClientSendQueue : IDisposable
{
    private readonly Stream _stream;
    private readonly Action _failed;
    private readonly Channel<PendingWrite> _queue;
    private readonly CancellationTokenSource _stop = new();
    private int _disposed;

    internal TcpClientSendQueue(Stream stream, Action failed, int capacity = 100)
    {
        _stream = stream;
        _failed = failed;
        _queue = Channel.CreateBounded<PendingWrite>(
            new BoundedChannelOptions(capacity)
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.Wait,
            }
        );
        Completion = WriteAsync(_stop.Token);
    }

    internal Task Completion { get; }

    internal bool TrySend(ReadOnlySpan<byte> data)
    {
        return _queue.Writer.TryWrite(
            new PendingWrite(data.ToArray(), null, CancellationToken.None)
        );
    }

    internal async Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancel)
    {
        var completion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var write = new PendingWrite(data.ToArray(), completion, cancel);
        await _queue.Writer.WriteAsync(write, cancel).ConfigureAwait(false);
        await completion.Task.WaitAsync(cancel).ConfigureAwait(false);
    }

    private async Task WriteAsync(CancellationToken cancel)
    {
        try
        {
            await foreach (var write in _queue.Reader.ReadAllAsync(cancel).ConfigureAwait(false))
            {
                if (write.Cancel.IsCancellationRequested)
                {
                    write.Completion?.TrySetCanceled(write.Cancel);
                    continue;
                }
                try
                {
                    if (write.Cancel.CanBeCanceled)
                    {
                        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                            cancel,
                            write.Cancel
                        );
                        await _stream.WriteAsync(write.Data, linked.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        await _stream.WriteAsync(write.Data, cancel).ConfigureAwait(false);
                    }
                    write.Completion?.TrySetResult();
                }
                catch (Exception ex)
                {
                    write.Completion?.TrySetException(ex);
                    throw;
                }
            }
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested) { }
        catch (Exception)
        {
            _failed();
        }
        finally
        {
            _queue.Writer.TryComplete();
            while (_queue.Reader.TryRead(out var write))
            {
                write.Completion?.TrySetCanceled();
            }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }
        _queue.Writer.TryComplete();
        _stop.Cancel();
        _ = DisposeCancellationAsync();
    }

    private async Task DisposeCancellationAsync()
    {
        try
        {
            await Completion.ConfigureAwait(false);
        }
        finally
        {
            _stop.Dispose();
        }
    }

    private sealed class PendingWrite(
        byte[] data,
        TaskCompletionSource? completion,
        CancellationToken cancel
    )
    {
        public byte[] Data { get; } = data;
        public TaskCompletionSource? Completion { get; } = completion;
        public CancellationToken Cancel { get; } = cancel;
    }
}
