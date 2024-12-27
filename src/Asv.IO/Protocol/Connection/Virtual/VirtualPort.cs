using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using R3;
using OperationCanceledException = System.OperationCanceledException;

namespace Asv.IO;

public class VirtualPort:ProtocolConnection, IProtocolEndpoint
{
    private Func<IProtocolMessage, bool> _sendFilter;
    private readonly Subject<byte[]> _tx = new();
    private readonly Subject<byte[]> _rx = new();
    private readonly ImmutableArray<IProtocolParser> _parsers;
    private readonly IDisposable _dispose;
    private readonly ReactiveProperty<ProtocolConnectionException?> _lastError = new();

    public VirtualPort(string id, IProtocolContext context, Func<IProtocolMessage, bool> sendFilter, IStatisticHandler parent)
        : base(id, context,parent)
    {
        _sendFilter = sendFilter;
        _parsers = [..context.ParserFactory.Values.Select(x => x(context,StatisticHandler))];
        var builder = Disposable.CreateBuilder();
        foreach (var parser in _parsers)
        {
            parser.OnMessage
                .Do(_ => StatisticHandler.IncrementRxMessage())
                .Subscribe(InternalPublishRxMessage).AddTo(ref builder);
        }
        _rx.Subscribe(ReadLoop).AddTo(ref builder);
        _dispose = builder.Build();
    }
    
    public ReadOnlyReactiveProperty<ProtocolConnectionException?> LastError => _lastError;
    public IEnumerable<IProtocolParser> Parsers => _parsers;
    
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

    public void SetTxFilter(Func<IProtocolMessage, bool> filter)
    {
        _sendFilter = filter;
    }
    
    public override ValueTask Send(IProtocolMessage message, CancellationToken cancel = default)
    {
        if (cancel.IsCancellationRequested)
        {
            return ValueTask.FromException(new OperationCanceledException());
        }

        if (IsDisposed)
        {
            return ValueTask.CompletedTask;
        }

        if (_sendFilter(message) == false)
        {
            return ValueTask.CompletedTask;
        }
        
        try
        {
            var size = message.GetByteSize();
            var buffer = new byte[size];
            var span = buffer.AsSpan();
            message.Serialize(ref span);
            _tx.OnNext(buffer);
            StatisticHandler.IncrementTxMessage();
            InternalPublishTxMessage(message);
        }
        catch (Exception e)
        {
            StatisticHandler.IncrementTxError();
        }
        return ValueTask.CompletedTask;
    }
    
    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tx.Dispose();
            _rx.Dispose();
            _lastError.Dispose();
            _dispose.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await CastAndDispose(_tx);
        await CastAndDispose(_rx);
        await CastAndDispose(_lastError);
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