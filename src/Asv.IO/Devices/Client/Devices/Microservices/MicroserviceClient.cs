using System;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;

namespace Asv.IO;



public abstract class MicroserviceClient<TBaseMessage> : AsyncDisposableWithCancel, IMicroserviceClient
    where TBaseMessage : IProtocolMessage
{
    public delegate bool FilterDelegate<TResult>(TBaseMessage inputPacket, out TResult result);
    
    
    private readonly Subject<TBaseMessage> _internalFilteredDeviceMessages = new();
    private readonly IDisposable _sub1;
    private readonly ILogger _loggerBase;

    protected MicroserviceClient(IDeviceContext context, string id)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(id);
        Context = context;
        Id = id;
        _loggerBase = context.LoggerFactory.CreateLogger(id);
        _sub1 = context.Connection.RxFilterByType<TBaseMessage>().Where(FilterDeviceMessages)
            .Subscribe(_internalFilteredDeviceMessages.AsObserver());
    }

    public string Id { get; }
    protected IDeviceContext Context { get; }

    public virtual Task Init(CancellationToken cancel = default)
    {
        return Task.CompletedTask;
    }

    protected abstract void FillMessageBeforeSent(TBaseMessage message);
    
    protected abstract bool FilterDeviceMessages(TBaseMessage arg);
    
    protected Observable<TMessage> InternalFilter<TMessage>()
        where TMessage:TBaseMessage
    {
        return _internalFilteredDeviceMessages
            .Where(raw => raw is TMessage).Cast<TBaseMessage, TMessage>();
    }
    protected Observable<TMessage> InternalFilter<TMessage>(Func<TMessage, bool> filter)
        where TMessage : TBaseMessage
    {
        return InternalFilter<TMessage>().Where(filter);
    }
    protected Observable<TMessage> InternalFilterFirstAsync<TMessage>(Func<TMessage, bool> filter)
        where TMessage : TBaseMessage
    {
        return InternalFilter(filter).Take(1);
    }
    
    protected Observable<TBaseMessage> InternalFilteredDeviceMessages => _internalFilteredDeviceMessages;
    
    protected ValueTask InternalSend<TMessage>(Action<TMessage> fillPacket, CancellationToken cancel = default)
        where TMessage : TBaseMessage, new()
    {
        ArgumentNullException.ThrowIfNull(fillPacket);
        var packet = new TMessage();
        fillPacket(packet);
        return InternalSend(packet, cancel);
    }
    protected ValueTask InternalSend(TBaseMessage packet, CancellationToken cancel = default)
    {
        ArgumentNullException.ThrowIfNull(packet);
        cancel.ThrowIfCancellationRequested();
        _loggerBase.ZLogTrace($"=> send {packet.Name}");
        FillMessageBeforeSent(packet);
        return Context.Connection.Send(packet, cancel);
    }

    protected async Task<TResult> InternalSendAndWaitAnswer<TResult>(TBaseMessage packet,
        FilterDelegate<TResult> filterAndResultGetter, int timeoutMs = 1000,
        CancellationToken cancel = default)
    {
        cancel.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(filterAndResultGetter);
        _loggerBase.ZLogTrace($"=> Send {packet.Name} and wait for answer {nameof(TResult)} with timeout {timeoutMs} ms");
        using var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel, DisposeCancel);
        linkedCancel.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs), Context.TimeProvider);
        var tcs = new TaskCompletionSource<TResult>();
        await using var c1 = linkedCancel.Token.Register(() => tcs.TrySetCanceled(), false);

        using var subscribe = InternalFilteredDeviceMessages.Subscribe(x=>
        {
            if (filterAndResultGetter(x, out var result))
            {
                tcs.TrySetResult(result);
            }
        });
        
        var result = await tcs.Task.ConfigureAwait(false);
        _loggerBase.ZLogTrace($"<= ok {packet.Name}<=={result}");
        return result;
    }
    protected async Task<TAnswerPacket> InternalSendAndWaitAnswer<TAnswerPacket>(TBaseMessage packet,
        CancellationToken cancel, Func<TAnswerPacket, bool>? filter = null, int timeoutMs = 1000)
        where TAnswerPacket : TBaseMessage, new()
    {
        cancel.ThrowIfCancellationRequested();
        var p = new TAnswerPacket();
        _loggerBase.ZLogTrace($"=> call {p.Name} and wait for answer with timeout {timeoutMs} ms");
        using var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel, DisposeCancel);
        linkedCancel.CancelAfter(timeoutMs, Context.TimeProvider);
        var tcs = new TaskCompletionSource<TAnswerPacket>();
        await using var c1 = linkedCancel.Token.Register(() => tcs.TrySetCanceled(), false);

        filter ??= (_ => true);
        using var subscribe = InternalFilterFirstAsync(filter).Subscribe(v=>tcs.TrySetResult(v));
        await InternalSend(packet, linkedCancel.Token);
        var result = await tcs.Task.ConfigureAwait(false);
        _loggerBase.ZLogTrace($"<= ok {packet.Name}<=={p.Name}");
        return result;
    }
    
    protected async Task<TResult> InternalCall<TResult,TPacketSend>(
        Action<TPacketSend> fillPacket, FilterDelegate<TResult> filterAndResultGetter, int attemptCount = 5,
        Action<TPacketSend,int>? fillOnConfirmation = null, int timeoutMs = 1000,  CancellationToken cancel = default)
        where TPacketSend : TBaseMessage, new()
    {
        cancel.ThrowIfCancellationRequested();
        var packet = new TPacketSend();
        fillPacket(packet);
        byte currentAttempt = 0;
        var name = packet.Name;
        while (IsRetryCondition())
        {
            if (currentAttempt != 0)
            {
                fillOnConfirmation?.Invoke(packet, currentAttempt);
                _loggerBase.ZLogWarning($"=> replay {currentAttempt} {name}");
            }
            ++currentAttempt;
            try
            {
                return await InternalSendAndWaitAnswer(packet, filterAndResultGetter, timeoutMs, cancel).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (IsRetryCondition())
                {
                    continue;
                }

                cancel.ThrowIfCancellationRequested();
            }
        }
        _loggerBase.ZLogError($"Timeout to execute '{name}' with {attemptCount} x {timeoutMs} ms'");
        throw new TimeoutException($"Timeout to execute '{name}' with {attemptCount} x {timeoutMs} ms'");
        bool IsRetryCondition() => currentAttempt < attemptCount;
    }
    
    protected async Task<TResult> InternalCall<TResult,TPacketSend,TPacketRecv>(
        Action<TPacketSend> fillPacket, Func<TPacketRecv,bool>? filter, Func<TPacketRecv,TResult> resultGetter, int attemptCount = 5,
        Action<TPacketSend,int>? fillOnConfirmation = null, int timeoutMs = 1000, CancellationToken cancel = default)
        where TPacketSend : TBaseMessage, new()
        where TPacketRecv : TBaseMessage, new()
    {
        cancel.ThrowIfCancellationRequested();
        var packet = new TPacketSend();
        fillPacket(packet);
        byte currentAttempt = 0;
        TPacketRecv? result = default;
        var name = packet.Name;
        while (IsRetryCondition())
        {
            if (currentAttempt != 0)
            {
                fillOnConfirmation?.Invoke(packet, currentAttempt);
                _loggerBase.ZLogWarning($"=> replay {currentAttempt} {name}");
            }
            ++currentAttempt;
            try
            {
                result = await InternalSendAndWaitAnswer(packet, cancel, filter, timeoutMs).ConfigureAwait(false);
                break;
            }
            catch (OperationCanceledException)
            {
                if (IsRetryCondition())
                {
                    continue;
                }
                    
                cancel.ThrowIfCancellationRequested();
            }
        }

        if (result != null) return resultGetter(result);
        _loggerBase.ZLogError($"Timeout to execute '{name}' with {attemptCount} x {timeoutMs} ms'");
        throw new TimeoutException($"Timeout to execute '{name}' with {attemptCount} x {timeoutMs} ms'");
        bool IsRetryCondition() => currentAttempt < attemptCount;
    }
    
    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _internalFilteredDeviceMessages.Dispose();
            _sub1.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await CastAndDispose(_internalFilteredDeviceMessages);
        await CastAndDispose(_sub1);

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