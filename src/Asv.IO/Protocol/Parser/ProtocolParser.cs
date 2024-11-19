using System;
using System.Collections.Immutable;
using System.Threading;
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
    private uint _readBytes;
    private uint _readMessages;

    protected ProtocolParser(IProtocolMessageFactory<TMessage,TMessageId> messageFactory, IProtocolCore core)
    {
        ArgumentNullException.ThrowIfNull(messageFactory);
        _messageFactory = messageFactory;
        Tags = new ProtocolTags();
    }
    public uint StatRxBytes => _readBytes;
    public uint StatRxMessages => _readMessages;
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
            return;
        }
        try
        {
            var count = data.Length;
            message.Deserialize(ref data);
            Interlocked.Add(ref _readBytes, (uint)(count - data.Length));
        }
        catch (Exception e)
        {
            InternalOnError(new ProtocolDeserializeMessageException(Info, message, e));
            return;
        }
        Interlocked.Increment(ref _readMessages);
        try
        {
            _onMessage.OnNext(message);
        }
        catch (Exception e)
        {
            InternalOnError(new ProtocolPublishMessageException(Info, message, e));
        }

        if (!ignoreReadNotAllData && !data.IsEmpty)
        {
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

