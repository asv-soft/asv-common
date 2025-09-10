using System;
using System.Threading;
using System.Threading.Tasks;
using R3;
using ZLogger;

namespace Asv.IO;

public sealed class VirtualConnection : IVirtualConnection
{
    private readonly VirtualPort _server;
    private readonly VirtualPort _client;
    private readonly IDisposable _sub1;
    private readonly IDisposable _sub2;
    private readonly Statistic _statistic = new();
    private readonly IDisposable? _sub3;
    private readonly IDisposable? _sub4;

    public VirtualConnection(
        Func<IProtocolMessage, bool>? clientToServerFilter,
        Func<IProtocolMessage, bool>? serverToClientFilter,
        IProtocolContext context,
        bool printMessages = true
    )
    {
        clientToServerFilter ??= _ => true;
        serverToClientFilter ??= _ => true;
        _server = new VirtualPort("Server", context, serverToClientFilter, _statistic);
        _client = new VirtualPort("Client", context, clientToServerFilter, _statistic);

        _sub1 = _server.Tx.Subscribe(_client.Rx);
        _sub2 = _client.Tx.Subscribe(_server.Rx);
        if (printMessages)
        {
            var serverToClient = context.LoggerFactory.CreateLogger("SERVER=>CLIENT");
            var scInx = 0;
            _sub3 = Client.OnRxMessage.Subscribe(
                serverToClient,
                (x, log) =>
                    log.ZLogTrace(
                        $"[{Interlocked.Increment(ref scInx):000}] {context.PrintMessage(x)}"
                    )
            );
            var clientToServer = context.LoggerFactory.CreateLogger("CLIENT=>SERVER");
            var csInx = 0;
            _sub4 = Server.OnRxMessage.Subscribe(
                clientToServer,
                (x, log) =>
                    log.ZLogTrace(
                        $"[{Interlocked.Increment(ref csInx):000}] {context.PrintMessage(x)}"
                    )
            );
        }
    }

    public void SetClientToServerFilter(Func<IProtocolMessage, bool> filter)
    {
        _client.SetTxFilter(filter);
    }

    public void SetServerToClientFilter(Func<IProtocolMessage, bool> filter)
    {
        _server.SetTxFilter(filter);
    }

    public IStatistic Statistic => _statistic;
    public IProtocolConnection Server => _server;

    public IProtocolConnection Client => _client;

    #region Dispose

    public void Dispose()
    {
        _server.Dispose();
        _client.Dispose();
        _sub1.Dispose();
        _sub2.Dispose();
        _sub3?.Dispose();
        _sub4?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _server.DisposeAsync();
        await _client.DisposeAsync();
        await CastAndDispose(_sub1);
        await CastAndDispose(_sub2);
        if (_sub3 != null)
        {
            await CastAndDispose(_sub3);
        }

        if (_sub4 != null)
        {
            await CastAndDispose(_sub4);
        }

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
