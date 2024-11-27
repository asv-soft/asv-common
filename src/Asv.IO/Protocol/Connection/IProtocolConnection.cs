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
    
}

public delegate bool FilterDelegate<TResult, in TMessage,TMessageId>(TMessage input, out TResult result)
    where TMessage: IProtocolMessage<TMessageId>;

public delegate bool ResendMessageModifyDelegate<in TMessage,TMessageId>(TMessage input, int attempt)
    where TMessage: IProtocolMessage<TMessageId>;