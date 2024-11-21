using System;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Asv.IO;

public interface IProtocolConnection:ISupportTag
{
    string Id { get; }
    uint StatRxBytes { get; }
    uint StatTxBytes { get; }
    uint StatRxMessages { get; }
    uint StatTxMessages { get; }
    uint RxError { get; }
    uint TxError { get; }
    Observable<IProtocolMessage> OnRxMessage { get; }
    Observable<IProtocolMessage> OnTxMessage { get; }
    ValueTask Send(IProtocolMessage message, CancellationToken cancel = default);
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
}