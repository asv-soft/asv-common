using System;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Asv.IO;

public interface IProtocolConnection
    : ISupportTag,
        ISupportStatistic,
        IMessageFormatter,
        IDisposable,
        IAsyncDisposable
{
    string Id { get; }

    /// <summary>Gets a compact human-readable label. It must not be used as an identifier.</summary>
    string DisplayName => Id;

    /// <summary>Gets transport details suitable for a tooltip or diagnostics.</summary>
    string Description => Id;

    Observable<IProtocolMessage> OnTxMessage { get; }
    Observable<Exception> OnTxError { get; }
    Observable<IProtocolMessage> OnRxMessage { get; }
    Observable<Exception> OnRxError { get; }
    ValueTask Send(IProtocolMessage message, CancellationToken cancel = default);
}

public static class ProtocolConnectionHelper { }

public delegate bool FilterDelegate<TResult, in TMessage, TMessageId>(
    TMessage input,
    out TResult result
)
    where TMessage : IProtocolMessage<TMessageId>;

public delegate bool ResendMessageModifyDelegate<in TMessage, TMessageId>(
    TMessage input,
    int attempt
)
    where TMessage : IProtocolMessage<TMessageId>;
