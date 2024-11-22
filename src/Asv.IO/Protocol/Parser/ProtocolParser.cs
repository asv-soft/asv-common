using System;
using System.Threading.Tasks;
using Asv.Common;
using R3;

namespace Asv.IO;

public abstract class ProtocolParser<TMessage,TMessageId> : AsyncDisposableOnce, IProtocolParser
    where TMessage : IProtocolMessage<TMessageId>
    where TMessageId : struct
{
    private readonly IProtocolMessageFactory<TMessage,TMessageId> _messageFactory;
    private readonly Subject<IProtocolMessage> _onMessage = new();

    protected ProtocolParser(IProtocolMessageFactory<TMessage,TMessageId> messageFactory, IProtocolCore core, IStatisticHandler? statisticHandler)
    {
        ArgumentNullException.ThrowIfNull(messageFactory);
        _messageFactory = messageFactory;
        Tags = new ProtocolTags();
        if (statisticHandler == null)
        {
            var value = new Statistic();
            Statistic = value;
            StatisticHandler = value;
        }
        else
        {
            var value = new InheritedStatistic(statisticHandler);
            Statistic = value;
            StatisticHandler = value;
        }
    }

    private IStatisticHandler StatisticHandler { get; }
    public IStatistic Statistic { get; }
    public abstract ProtocolInfo Info { get; }
    public ProtocolTags Tags { get; }
    public Observable<IProtocolMessage> OnMessage => _onMessage;
    public abstract bool Push(byte data);
    public abstract void Reset();
    protected void InternalParsePacket(TMessageId id, ref ReadOnlySpan<byte> data, bool ignoreReadNotAllData = false)
    {
        var message = _messageFactory.Create(id);
        if (message == null)
        {
            InternalOnError(new ProtocolParserUnknownMessageException(Info,id));
            StatisticHandler.IncrementParserUnknownMessageError();
            return;
        }

        try
        {
            var count = data.Length;
            message.Deserialize(ref data);
            StatisticHandler.AddParserBytes(count);
        }
        catch (ProtocolParserCrcException ex)
        {
            InternalOnError(ex);
            StatisticHandler.IncrementParserBadCrcError();
        }
        catch (Exception e)
        {
            StatisticHandler.IncrementParserDeserializeError();
            InternalOnError(new ProtocolDeserializeMessageException(Info, message, e));
            return;
        }
        try
        {
            StatisticHandler.IncrementParsedMessage();
            _onMessage.OnNext(message);
        }
        catch (Exception e)
        {
            StatisticHandler.IncrementParserPublishError();
            InternalOnError(new ProtocolPublishMessageException(Info, message, e));
        }

        if (!ignoreReadNotAllData && !data.IsEmpty)
        {
            StatisticHandler.IncrementParserReadNotAllDataError();
            InternalOnError(new ProtocolParserReadNotAllDataWhenDeserializePacketException(Info, message));
        }
    }

    protected void InternalOnError(ProtocolParserException ex)
    {
        _onMessage.OnErrorResume(ex);
    }

    #region Dispose


    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _onMessage.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_onMessage is IAsyncDisposable onMessageAsyncDisposable)
            await onMessageAsyncDisposable.DisposeAsync();
        else
            _onMessage.Dispose();

        await base.DisposeAsyncCore();
    }

    #endregion
}

