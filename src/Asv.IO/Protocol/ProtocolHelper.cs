using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using R3;

namespace Asv.IO;

public static partial class ProtocolHelper
{

    internal static string NormalizeId(string id) => IdNormalizeRegex.Replace(id, "_");

    [GeneratedRegex(@"[^\w]")]
    private static partial Regex MyRegex();
    private static readonly Regex IdNormalizeRegex = MyRegex();


    public static IProtocolParser CreateParser(this IProtocolFactory src, ProtocolInfo info) => src.CreateParser(info.Id);

    public static Observable<TMessage> RxFilterByType<TMessage>(this IProtocolConnection connection)
        where TMessage : IProtocolMessage => connection.OnRxMessage.FilterByType<TMessage>();
    
    public static Observable<TMessage> TxFilterByType<TMessage>(this IProtocolConnection connection)
        where TMessage : IProtocolMessage => connection.OnTxMessage.FilterByType<TMessage>();
    public static Observable<TMessage> RxFilterByType<TMessage>(this IProtocolConnection connection, Func<TMessage, bool> filter)
        where TMessage : IProtocolMessage => connection.OnRxMessage.FilterByType(filter);
    
    public static Observable<TMessage> TxFilterByType<TMessage>(this IProtocolConnection connection, Func<TMessage, bool> filter)
        where TMessage : IProtocolMessage => connection.OnTxMessage.FilterByType(filter);

    public static Observable<TMessage> FilterByType<TMessage>(this Observable<IProtocolMessage> src)
        where TMessage : IProtocolMessage
    {
        return src.Where(raw => raw is TMessage).Cast<IProtocolMessage, TMessage>();
    }
    
    public static Observable<TMessage> FilterByType<TMessage>(this Observable<IProtocolMessage> src, Func<TMessage, bool> filter)
        where TMessage : IProtocolMessage
    {
        return src.Where(raw => raw is TMessage)
            .Cast<IProtocolMessage, TMessage>()
            .Where(filter);
    }

    public static Observable<TMessage> FilterByMsgId<TMessage, TMessageId>(this Observable<IProtocolMessage> src)
        where TMessage : IProtocolMessage<TMessageId>, new()
    {
        var messageId = new TMessage().Id;
        return src.Where(messageId, (raw, id) =>
        {
            if (raw is TMessage message)
            {
                return message.Id != null && message.Id.Equals(id);
            }
            return false;

        }).Cast<IProtocolMessage, TMessage>();
    }
    public static Observable<TMessage> FilterByMsgId<TMessage, TMessageId>(this Observable<IProtocolMessage> src,
        Func<TMessage, bool> filter)
        where TMessage : IProtocolMessage<TMessageId>, new()
    {
        return src.FilterByMsgId<TMessage, TMessageId>().Where(filter);
    }
    
    public static Observable<TMessage> RxFilterByMsgId<TMessage, TMessageId>(this IProtocolConnection connection)
        where TMessage : IProtocolMessage<TMessageId>, new() 
        => connection.OnRxMessage.FilterByMsgId<TMessage, TMessageId>();
    
    public static Observable<TMessage> TxFilterByMsgId<TMessage, TMessageId>(this IProtocolConnection connection)
        where TMessage : IProtocolMessage<TMessageId>, new() 
        => connection.OnTxMessage.FilterByMsgId<TMessage, TMessageId>();

    public static Observable<TMessage> RxFilterById<TMessage, TMessageId>(this IProtocolConnection connection,
        Func<TMessage, bool> filter)
        where TMessage : IProtocolMessage<TMessageId>, new() 
        => connection.OnRxMessage.FilterByMsgId<TMessage, TMessageId>(filter);
    public static Observable<TMessage> TxFilterById<TMessage, TMessageId>(this IProtocolConnection connection,
        Func<TMessage, bool> filter)
        where TMessage : IProtocolMessage<TMessageId>, new() 
        => connection.OnTxMessage.FilterByMsgId<TMessage, TMessageId>(filter);
    public static async Task<TResult> SendAndWaitAnswer<TResult, TMessage, TMessageId>(
        this IProtocolConnection connection,
        IProtocolMessage request,
        FilterDelegate<TResult, TMessage, TMessageId> filterAndGetResult,
        CancellationToken cancel = default)
        where TMessage : IProtocolMessage<TMessageId>, new()
    {
        cancel.ThrowIfCancellationRequested();
        var tcs = new TaskCompletionSource<TResult>();
        await using var c1 = cancel.Register(() => tcs.TrySetCanceled());
        using var c2 = connection.RxFilterByMsgId<TMessage, TMessageId>()
            .Subscribe(filterAndGetResult, (res, f) =>
        {
            if (filterAndGetResult(res, out var result))
            {
                tcs.TrySetResult(result);
            }
        });
        await connection.Send(request, cancel).ConfigureAwait(false);
        return await tcs.Task.ConfigureAwait(false);
    }

    public static async Task<TResult> SendAndWaitAnswer<TResult, TMessage, TMessageId>(
        this IProtocolConnection connection,
        IProtocolMessage request,
        FilterDelegate<TResult, TMessage, TMessageId> filterAndGetResult,
        TimeSpan timeout,
        CancellationToken cancel = default,
        TimeProvider? timeProvider = null)
        where TMessage : IProtocolMessage<TMessageId>, new()
    {
        timeProvider ??= TimeProvider.System;
        using var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        linkedCancel.CancelAfter(timeout, timeProvider);
        return await connection.SendAndWaitAnswer(request, filterAndGetResult, cancel);
    }

    public static async Task<TResult> SendAndWaitAnswer<TResult, TRequestMessage, TResultMessage, TMessageId>(
        this IProtocolConnection connection,
        TRequestMessage request,
        FilterDelegate<TResult, TResultMessage, TMessageId> filterAndGetResult,
        TimeSpan timeout,
        int attemptCount,
        ResendMessageModifyDelegate<TRequestMessage, TMessageId>? modifyRequestOnResend = null,
        CancellationToken cancel = default,
        TimeProvider? timeProvider = null,
        IProgress<int>? progress = null)
        where TResultMessage : IProtocolMessage<TMessageId>, new()
        where TRequestMessage : IProtocolMessage<TMessageId>
    {
        cancel.ThrowIfCancellationRequested();
        TResult? result = default;
        byte currentAttempt = 0;
        progress ??= new Progress<int>();
        while (IsRetryCondition())
        {
            progress.Report(currentAttempt);
            if (currentAttempt != 0)
            {
                modifyRequestOnResend?.Invoke(request, currentAttempt);
            }

            ++currentAttempt;
            try
            {
                result = await connection.SendAndWaitAnswer(request, filterAndGetResult, timeout, cancel, timeProvider)
                    .ConfigureAwait(false);
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
        if (result != null) return result;
        throw new TimeoutException($"Timeout to execute '{request}' with {attemptCount} x {timeout}'");
        
        bool IsRetryCondition() => currentAttempt < attemptCount;
    }
    
}