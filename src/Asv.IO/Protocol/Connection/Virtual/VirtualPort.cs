using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using R3;

namespace Asv.IO;

public class VirtualPort : ProtocolConnection, IProtocolEndpoint
{
    private Func<IProtocolMessage, bool> _sendFilter;
    private readonly Subject<byte[]> _tx = new();
    private readonly Subject<byte[]> _rx = new();
    private readonly ImmutableArray<IProtocolParser> _parsers;
    private readonly IDisposable _dispose;
    private readonly ReactiveProperty<ProtocolConnectionException?> _lastError = new();
    private readonly ProtocolEncoderSet _encoders;
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private readonly Lock _parserSync = new();
    private int _reading;
    private bool _parsersDisposed;

    public VirtualPort(
        string id,
        IProtocolContext context,
        Func<IProtocolMessage, bool> sendFilter,
        IStatisticHandler parent
    )
        : base(id, context, parent)
    {
        _sendFilter = sendFilter;
        _encoders = new ProtocolEncoderSet(context);
        _parsers = [.. context.ParserFactory.Values.Select(x => x(context, StatisticHandler))];
        var builder = Disposable.CreateBuilder();
        foreach (var parser in _parsers)
        {
            parser
                .OnMessage.Do(_ => StatisticHandler.IncrementRxMessage())
                .Subscribe(InternalPublishRxMessage)
                .AddTo(ref builder);
        }
        _rx.Subscribe(ReadLoop).AddTo(ref builder);
        _dispose = builder.Build();
    }

    public ReadOnlyReactiveProperty<ProtocolConnectionException?> LastError => _lastError;
    public IEnumerable<IProtocolParser> Parsers => _parsers;

    private void ReadLoop(byte[] bytes)
    {
        lock (_parserSync)
        {
            if (IsDisposed)
            {
                return;
            }
            _reading++;
            try
            {
                foreach (var b in bytes)
                {
                    if (IsDisposed)
                    {
                        break;
                    }
                    foreach (var parser in _parsers)
                    {
                        if (IsDisposed)
                        {
                            break;
                        }
                        if (parser.Push(b))
                        {
                            if (!IsDisposed)
                            {
                                _parsers.ForEach(x => x.Reset());
                            }
                            break;
                        }
                    }
                }
            }
            finally
            {
                _reading--;
                if (IsDisposed)
                {
                    DisposeParsers();
                }
            }
        }
    }

    private void DisposeParsers()
    {
        lock (_parserSync)
        {
            if (_reading != 0 || _parsersDisposed)
            {
                return;
            }
            _parsersDisposed = true;
            foreach (var parser in _parsers)
            {
                parser.Dispose();
            }
        }
    }

    public Observer<byte[]> Rx => _rx.AsObserver();
    public Observable<byte[]> Tx => _tx;

    public void SetTxFilter(Func<IProtocolMessage, bool> filter)
    {
        _sendFilter = filter;
    }

    public override async ValueTask Send(
        IProtocolMessage message,
        CancellationToken cancel = default
    )
    {
        ArgumentNullException.ThrowIfNull(message);
        cancel.ThrowIfCancellationRequested();

        if (IsDisposed || _lastError.CurrentValue != null)
        {
            return;
        }

        // Retain the token: DisposeCancel returns None after the port is disposed.
        var disposeCancel = ConnectionCancel;
        if (!await _sendGate.WaitAsync(0, cancel).ConfigureAwait(false))
        {
            if (IsDisposed)
            {
                return;
            }

            using var linkedCancel = cancel.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancel, disposeCancel)
                : null;
            await _sendGate.WaitAsync(linkedCancel?.Token ?? disposeCancel).ConfigureAwait(false);
        }
        try
        {
            // A semaphore grant can win the race with cancellation of its waiter.
            cancel.ThrowIfCancellationRequested();
            disposeCancel.ThrowIfCancellationRequested();
            if (IsDisposed || _lastError.CurrentValue != null || _sendFilter(message) == false)
            {
                return;
            }

            var newMessage = await InternalFilterTxMessage(message).ConfigureAwait(false);
            if (newMessage == null)
            {
                return;
            }

            cancel.ThrowIfCancellationRequested();
            disposeCancel.ThrowIfCancellationRequested();
            try
            {
                var encoder = _encoders.Get(newMessage.Protocol.Id);
                var buffer = newMessage.EncodeToArray(encoder);
                _tx.OnNext(buffer);
                StatisticHandler.AddTxBytes(buffer.Length);
                StatisticHandler.IncrementTxMessage();
                InternalPublishTxMessage(newMessage);
            }
            catch (Exception e)
            {
                StatisticHandler.IncrementTxError();
                var error = new ProtocolConnectionException(
                    this,
                    $"Error sending message: {e.Message}",
                    e
                );
                InternalPublishTxError(error);
                if (!IsDisposed)
                {
                    _lastError.OnNext(error);
                }
            }
        }
        finally
        {
            if (IsDisposed)
            {
                _encoders.Clear();
            }
            _sendGate.Release();
        }
    }

    private void ClearIdleEncoders()
    {
        if (_sendGate.Wait(0))
        {
            try
            {
                _encoders.Clear();
            }
            finally
            {
                _sendGate.Release();
            }
        }
    }

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CancelPendingOperations();
            _tx.Dispose();
            _rx.Dispose();
            _lastError.Dispose();
            _dispose.Dispose();
            DisposeParsers();
            ClearIdleEncoders();
        }
        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        CancelPendingOperations();
        await CastAndDispose(_tx);
        await CastAndDispose(_rx);
        await CastAndDispose(_lastError);
        await CastAndDispose(_dispose);
        DisposeParsers();
        ClearIdleEncoders();
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
