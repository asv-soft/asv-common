using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using R3;

namespace Asv.IO;

public sealed class VirtualConnection: IVirtualConnection
{
    private readonly VirtualPort _server;
    private readonly VirtualPort _client;
    private readonly IDisposable _sub1;
    private readonly IDisposable _sub2;

    public VirtualConnection(Func<IProtocolMessage, bool>? clientToServerFilter,Func<IProtocolMessage, bool>? serverToClientFilter, IProtocolContext context)
    {
        clientToServerFilter ??= (_ => true);
        serverToClientFilter ??= (_ => true);
        _server = new VirtualPort("Server", context,serverToClientFilter);
        _client = new VirtualPort("Client", context,clientToServerFilter);
        _sub1 = _server.Tx.Subscribe(_client.Rx);
        _sub2 = _client.Tx.Subscribe(_server.Rx);
    }

    public IProtocolConnection Server => _server;

    public IProtocolConnection Client => _client;

    #region Dispose

    public void Dispose()
    {
        _server.Dispose();
        _client.Dispose();
        _sub1.Dispose();
        _sub2.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _server.DisposeAsync();
        await _client.DisposeAsync();
        await CastAndDispose(_sub1);
        await CastAndDispose(_sub2);

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

public class VirtualPort:ProtocolConnection
{
    private readonly Func<IProtocolMessage, bool> _sendFilter;
    private readonly Subject<byte[]> _tx = new();
    private readonly Subject<byte[]> _rx = new();
    private readonly ImmutableArray<IProtocolParser> _parsers;
    private readonly IDisposable _dispose;

    public VirtualPort(string id, IProtocolContext context, Func<IProtocolMessage, bool> sendFilter) : base(id, context)
    {
        _sendFilter = sendFilter;
        _parsers = [..context.ParserFactory.Values.Select(x => x(context,StatisticHandler))];
        var builder = Disposable.CreateBuilder();
        foreach (var parser in _parsers)
        {
            parser.OnMessage.Subscribe(InternalPublishRxMessage).AddTo(ref builder);
        }
        _rx.Subscribe(ReadLoop).AddTo(ref builder);
        _dispose = builder.Build();
    }

    private void ReadLoop(byte[] bytes)
    {
        foreach (var b in bytes)
        {
            foreach (var parser in _parsers)
            {
                if (parser.Push(b))
                {
                    _parsers.ForEach(x=>x.Reset());
                }
            }
        }
    }
    public Observer<byte[]> Rx => _rx.AsObserver();
    public Observable<byte[]> Tx => _tx;
    public override ValueTask Send(IProtocolMessage message, CancellationToken cancel = default)
    {
        if (_sendFilter(message) == false) return ValueTask.CompletedTask;
        var size = message.GetByteSize();
        var buffer = new byte[size];
        var span = buffer.AsSpan();
        message.Serialize(ref span);
        _tx.OnNext(buffer);
        return ValueTask.CompletedTask;
    }
    
    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tx.Dispose();
            _rx.Dispose();
            _dispose.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await CastAndDispose(_tx);
        await CastAndDispose(_rx);
        await CastAndDispose(_dispose);

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

