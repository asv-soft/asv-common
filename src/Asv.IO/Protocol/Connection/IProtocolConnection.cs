using System;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using R3;
using ZLogger;

namespace Asv.IO;

public interface IProtocolConnection:ISupportTag,ISupportStatistic, IDisposable, IAsyncDisposable
{
    string Id { get; }
    Observable<IProtocolMessage> OnTxMessage { get; }
    Observable<IProtocolMessage> OnRxMessage { get; }
    ValueTask Send(IProtocolMessage message, CancellationToken cancel = default);
    string? PrintMessage(IProtocolMessage message, PacketFormatting formatting = PacketFormatting.Inline);
}

public static class ProtocolConnectionHelper
{
    public static Observable<TMessage> RxFilter<TMessage, TMessageId>(this IProtocolConnection connection)
        where TMessage : IProtocolMessage<TMessageId>, new()
    {
        var messageId = new TMessage().Id;
        return connection.OnRxMessage.Where(messageId, (raw, id) =>
        {
            if (raw is TMessage message)
            {
                return message.Id != null && message.Id.Equals(id);
            }

            return false;

        }).Cast<IProtocolMessage, TMessage>();
    }

    public static Observable<TMessage> RxFilter<TMessage, TMessageId>(this IProtocolConnection connection,
        Func<TMessage, bool> filter)
        where TMessage : IProtocolMessage<TMessageId>, new()
    {
        var messageId = new TMessage().Id;
        return connection.OnRxMessage.Where(messageId, (raw, id) =>
        {
            if (raw is TMessage message)
            {
                return message.Id != null && message.Id.Equals(id) && filter(message);
            }

            return false;

        }).Cast<IProtocolMessage, TMessage>();
    }

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
        using var c2 = connection.RxFilter<TMessage, TMessageId>().Subscribe(filterAndGetResult, (res, f) =>
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
        int maxAttemptCount,
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
        throw new TimeoutException($"Timeout to execute '{request}' with {maxAttemptCount} x {timeout}'");
        
        bool IsRetryCondition() => currentAttempt < maxAttemptCount;
    }
}

public delegate bool FilterDelegate<TResult, in TMessage,TMessageId>(TMessage input, out TResult result)
    where TMessage: IProtocolMessage<TMessageId>;

public delegate bool ResendMessageModifyDelegate<in TMessage,TMessageId>(TMessage input, int attempt)
    where TMessage: IProtocolMessage<TMessageId>;