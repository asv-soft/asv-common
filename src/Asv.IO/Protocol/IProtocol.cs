using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using R3;

namespace Asv.IO;


public interface IProtocolContext
{
    ILoggerFactory LoggerFactory { get; }
    TimeProvider TimeProvider { get; }
    IMeterFactory MeterFactory { get; }
    ImmutableDictionary<string, ParserFactoryDelegate> ParserFactory { get; }
    ImmutableArray<IProtocolFeature> Features { get; }
    ImmutableArray<ProtocolInfo> AvailableProtocols { get; }
    ImmutableDictionary<string, PortFactoryDelegate> PortFactory { get; }
    ImmutableArray<PortTypeInfo> AvailablePortTypes { get; }
    ImmutableArray<IProtocolMessageFormatter> Formatters { get; }
    ChannelWriter<IProtocolMessage> RxChannel { get; }
    ChannelWriter<ProtocolException> ErrorChannel { get; }
}

public enum PacketFormatting
{
    Inline,
    Indented,
}

public interface IProtocol:IDisposable, IAsyncDisposable
{
    IStatistic Statistic { get; }
    ImmutableArray<PortTypeInfo> AvailablePortTypes { get; }
    ImmutableArray<ProtocolInfo> AvailableProtocols { get; }
    string? PrintMessage(IProtocolMessage message, PacketFormatting formatting = PacketFormatting.Inline);
    /// <summary>
    ///          userinfo       host      port
    ///          ┌──┴───┐ ┌──────┴──────┐ ┌┴─┐
    ///  tcp_s://john.doe@www.example.com:1234/forum/questions/?br=115200&timeout=1000#protocol=mavlink&name=Port1&enabled=true
    ///  └─┬─┘   └─────────────┬─────────────┘└───────┬───────┘ └────────────┬───────┘ └───────┬──────────────────────────────┘
    ///  scheme            authority                path                   query                 fragment
    /// </summary>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    IProtocolPort AddPort(Uri connectionString);
    void RemovePort(IProtocolPort port);
    ImmutableArray<IProtocolPort> Ports { get; }
    Observable<ImmutableArray<IProtocolPort>> PortsChanged { get; }
    Observable<IProtocolMessage> OnTxMessage { get; }
    Observable<IProtocolMessage> OnRxMessage { get; }
    ValueTask Send(IProtocolMessage message, CancellationToken cancel = default);
}


public static partial class ProtocolHelper
{

    #region Internal

    internal static string NormalizeId(string id) => IdNormailizeRegex.Replace(id, "_");

    [GeneratedRegex(@"[^\w]")]
    private static partial Regex MyRegex();
    private static readonly Regex IdNormailizeRegex = MyRegex();

    #endregion
    
    public static IProtocolPort AddPort(this IProtocol src,string connectionString)
    {
        return src.AddPort(new Uri(connectionString));
    }
    
    public static Observable<TMessage> RxFilter<TMessage, TMessageId>(this IProtocol connection)
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
    
    public static Observable<TMessage> RxFilter<TMessage, TMessageId>(this IProtocol connection, Func<TMessage, bool> filter)
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