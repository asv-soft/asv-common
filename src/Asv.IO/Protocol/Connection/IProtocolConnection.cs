using System;
using System.Threading;
using System.Threading.Tasks;
using R3;

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
        where TMessage: IProtocolMessage<TMessageId>, new()
    {
        var messageId = new TMessage().Id;
        return connection.OnRxMessage.Where(messageId, (raw, id) =>
        {
            if (raw is TMessage message)
            {
                return message.Id != null && message.Id.Equals(id);
            }
            return false;

        }).Cast<IProtocolMessage,TMessage>();
    }
    
    public static Observable<TMessage> RxFilter<TMessage, TMessageId>(this IProtocolConnection connection, Func<TMessage, bool> filter)
        where TMessage: IProtocolMessage<TMessageId>, new()
    {
        var messageId = new TMessage().Id;
        return connection.OnRxMessage.Where(messageId, (raw, id) =>
        {
            if (raw is TMessage message)
            {
                return message.Id != null && message.Id.Equals(id) && filter(message);
            }
            return false;

        }).Cast<IProtocolMessage,TMessage>();
    }

    public static async Task<TResult> SendAndWaitAnswer<TResult,TMessage,TMessageId>(this IProtocolConnection connection, 
        IProtocolMessage send, 
        FilterDelegate<TResult, TMessage,TMessageId> filterAndGetResult,
        CancellationToken cancel = default)
        where TMessage: IProtocolMessage<TMessageId>, new()
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
        await connection.Send(send, cancel);
        return await tcs.Task.ConfigureAwait(false);
    }
    
    
}

public delegate bool FilterDelegate<TResult, in TMessage,TMessageId>(TMessage input, out TResult result)
    where TMessage: IProtocolMessage<TMessageId>;
