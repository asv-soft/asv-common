using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;

namespace Asv.IO;

public abstract class ProtocolConnection : AsyncDisposableWithCancel, IProtocolConnection
{
    private readonly ILogger<ProtocolConnection> _logger;
    private readonly Subject<IProtocolMessage> _onTxMessage = new();
    private readonly Subject<IProtocolMessage> _onRxMessage = new();
    private readonly Subject<Exception> _onRxError = new();
    private readonly Subject<Exception> _onTxError = new();
    private ProtocolTags _tags = [];
    private ImmutableArray<Func<IProtocolMessage, CancellationToken, ValueTask>> _rxHandlers = [];
    private readonly Lock _legacyRxSync = new();
    private Queue<IProtocolMessage>? _legacyRxQueue;
    private bool _legacyRxRunning;
    private const int LegacyRxQueueCapacity = 100;

    protected ProtocolConnection(
        string id,
        IProtocolContext context,
        IStatisticHandler? statistic = null
    )
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(context);
        _logger = context.LoggerFactory.CreateLogger<ProtocolConnection>();
        if (statistic == null)
        {
            var value = new Statistic();
            Statistic = value;
            StatisticHandler = value;
        }
        else
        {
            var value = new InheritedStatistic(statistic);
            Statistic = value;
            StatisticHandler = value;
        }
        Id = id;
        Context = context;
        ConnectionCancel = DisposeCancel;
        foreach (var feature in Context.Features)
        {
            feature.Register(this);
        }
    }

    public string Id { get; }
    public virtual string DisplayName => Id;
    public virtual string Description => Id;
    public IStatistic Statistic { get; }
    public Observable<IProtocolMessage> OnTxMessage => _onTxMessage;
    public Observable<Exception> OnTxError => _onTxError;
    public Observable<IProtocolMessage> OnRxMessage => _onRxMessage;
    public Observable<Exception> OnRxError => _onRxError;

    protected IStatisticHandler StatisticHandler { get; }
    protected IProtocolContext Context { get; }
    protected CancellationToken ConnectionCancel { get; }

    protected void CancelPendingOperations() => CancelDisposeToken();

    internal IDisposable SubscribeRx(Func<IProtocolMessage, CancellationToken, ValueTask> handler)
    {
        ImmutableInterlocked.Update(ref _rxHandlers, handlers => handlers.Add(handler));
        return Disposable.Create(() =>
            ImmutableInterlocked.Update(ref _rxHandlers, handlers => handlers.Remove(handler))
        );
    }

    public abstract ValueTask Send(IProtocolMessage message, CancellationToken cancel = default);

    public string? PrintMessage(
        IProtocolMessage message,
        PacketFormatting formatting = PacketFormatting.Inline
    ) => Context.PrintMessage(message, formatting);

    protected async ValueTask<IProtocolMessage?> InternalFilterTxMessage(IProtocolMessage message)
    {
        foreach (var item in Context.Features)
        {
            ConnectionCancel.ThrowIfCancellationRequested();
            var newMsg = await AwaitFeature(
                    item.ProcessTx(message, this, ConnectionCancel),
                    ConnectionCancel
                )
                .ConfigureAwait(false);
            if (newMsg == null)
            {
                return null;
            }

            message = newMsg;
        }
        return message;
    }

    // Compatibility entry point for synchronous publishers. Built-in async producers
    // await InternalPublishRxMessageAsync so congestion propagates back to their reader.
    protected void InternalPublishRxMessage(IProtocolMessage message)
    {
        lock (_legacyRxSync)
        {
            if (IsDisposed)
            {
                return;
            }
            if (_legacyRxRunning)
            {
                _legacyRxQueue ??= new Queue<IProtocolMessage>();
                if (_legacyRxQueue.Count < LegacyRxQueueCapacity)
                {
                    _legacyRxQueue.Enqueue(message);
                    return;
                }
                StatisticHandler.IncrementDropRxMessage();
                return;
            }
            _legacyRxRunning = true;
        }
        DrainLegacyRx(message);
    }

    private void DrainLegacyRx(IProtocolMessage message)
    {
        while (true)
        {
            var pending = InternalPublishRxMessageAsync(message, ConnectionCancel);
            if (!pending.IsCompletedSuccessfully)
            {
                _ = ContinueLegacyRxAsync(pending);
                return;
            }
            pending.GetAwaiter().GetResult();
            lock (_legacyRxSync)
            {
                if (
                    IsDisposed
                    || _legacyRxQueue == null
                    || !_legacyRxQueue.TryDequeue(out message!)
                )
                {
                    _legacyRxQueue?.Clear();
                    _legacyRxRunning = false;
                    return;
                }
            }
        }
    }

    private async Task ContinueLegacyRxAsync(ValueTask pending)
    {
        await pending.ConfigureAwait(false);
        IProtocolMessage message;
        lock (_legacyRxSync)
        {
            if (IsDisposed || _legacyRxQueue == null || !_legacyRxQueue.TryDequeue(out message!))
            {
                _legacyRxQueue?.Clear();
                _legacyRxRunning = false;
                return;
            }
        }
        DrainLegacyRx(message);
    }

    protected internal async ValueTask InternalPublishRxMessageAsync(
        IProtocolMessage message,
        CancellationToken cancel = default
    )
    {
        try
        {
            if (IsDisposed)
            {
                return;
            }
            if (!cancel.CanBeCanceled)
            {
                cancel = ConnectionCancel;
            }
            foreach (var item in Context.Features)
            {
                cancel.ThrowIfCancellationRequested();
                var newMsg = await AwaitFeature(item.ProcessRx(message, this, cancel), cancel)
                    .ConfigureAwait(false);
                if (newMsg == null)
                {
                    return;
                }

                message = newMsg;
            }
            cancel.ThrowIfCancellationRequested();
            ConnectionCancel.ThrowIfCancellationRequested();
            _onRxMessage.OnNext(message);
            foreach (var handler in _rxHandlers)
            {
                await AwaitHandler(handler(message, cancel), cancel).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
            when (cancel.IsCancellationRequested || ConnectionCancel.IsCancellationRequested) { }
        catch (ProtocolConnectionException ex)
        {
            InternalPublishRxError(ex);
        }
        catch (Exception ex)
        {
            InternalPublishRxError(
                new ProtocolConnectionException(
                    this,
                    $"Error at publish rx message:{ex.Message}",
                    ex
                )
            );
        }
    }

    private ValueTask<IProtocolMessage?> AwaitFeature(
        ValueTask<IProtocolMessage?> pending,
        CancellationToken cancel
    )
    {
        if (pending.IsCompletedSuccessfully)
        {
            return pending;
        }
        var task = pending.AsTask().WaitAsync(ConnectionCancel);
        if (cancel != ConnectionCancel)
        {
            task = task.WaitAsync(cancel);
        }
        return new ValueTask<IProtocolMessage?>(task);
    }

    private ValueTask AwaitHandler(ValueTask pending, CancellationToken cancel)
    {
        if (pending.IsCompletedSuccessfully)
        {
            return pending;
        }
        var task = pending.AsTask().WaitAsync(ConnectionCancel);
        if (cancel != ConnectionCancel)
        {
            task = task.WaitAsync(cancel);
        }
        return new ValueTask(task);
    }

    protected void InternalPublishRxError(Exception ex)
    {
        if (IsDisposed)
        {
            return;
        }

        _onRxError.OnNext(ex);
    }

    protected void InternalPublishTxError(ProtocolConnectionException ex)
    {
        if (IsDisposed)
        {
            return;
        }

        _onTxError.OnNext(ex);
    }

    protected void InternalPublishTxMessage(IProtocolMessage msg)
    {
        if (IsDisposed)
        {
            return;
        }

        _onTxMessage.OnNext(msg);
    }

    public ref ProtocolTags Tags => ref _tags;

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CancelPendingOperations();
            ClearReceiveHandlers();
            _logger.ZLogTrace($"{nameof(Dispose)} connection {Id}");
            foreach (var feature in Context.Features)
            {
                feature.Unregister(this);
            }
            _onTxError.Dispose();
            _onRxError.Dispose();
            _onTxMessage.Dispose();
            _onRxMessage.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        CancelPendingOperations();
        ClearReceiveHandlers();
        _logger.ZLogTrace($"{nameof(DisposeAsyncCore)} port {this}");
        foreach (var feature in Context.Features)
        {
            feature.Unregister(this);
        }
        await CastAndDispose(_onTxError).ConfigureAwait(false);
        await CastAndDispose(_onRxError).ConfigureAwait(false);
        await CastAndDispose(_onTxMessage).ConfigureAwait(false);
        await CastAndDispose(_onRxMessage).ConfigureAwait(false);
        await base.DisposeAsyncCore().ConfigureAwait(false);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                resource.Dispose();
            }
        }
    }

    private void ClearReceiveHandlers()
    {
        lock (_legacyRxSync)
        {
            _legacyRxQueue?.Clear();
        }
        _rxHandlers = [];
    }

    #endregion
}
