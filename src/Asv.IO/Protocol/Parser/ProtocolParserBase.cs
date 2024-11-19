using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Asv.IO;

public abstract class ProtocolParserBase<TMessage,TMessageId> : IProtocolParser
    where TMessage : IProtocolMessage
    where TMessageId : struct
{
    private readonly ImmutableDictionary<TMessageId, Func<TMessage>> _messageFactory;
    private readonly Subject<IProtocolMessage> _onMessage = new();
    private uint _readBytes;
    private uint _readMessages;

    protected ProtocolParserBase(ImmutableDictionary<TMessageId,Func<TMessage>> messageFactory)
    {
        ArgumentNullException.ThrowIfNull(messageFactory);
        _messageFactory = messageFactory;
        Tags = new ProtocolTags();
    }
    public uint StatRxBytes => _readBytes;
    public uint StatRxMessages => _readMessages;
    public abstract ProtocolParserInfo Info { get; }
    public ProtocolTags Tags { get; }
    public Observable<IProtocolMessage> OnMessage => _onMessage;
    public abstract bool Push(byte data);
    public abstract void Reset();
    protected void InternalParsePacket(TMessageId id, ref ReadOnlySpan<byte> data, bool ignoreReadNotAllData = false)
    {
        if (!_messageFactory.TryGetValue(id, out var factory))
        {
            _onMessage.OnErrorResume(new ProtocolParserUnknownMessageException(Info,id));
            return;
        }
        var message = factory();
        try
        {
            var count = data.Length;
            message.Deserialize(ref data);
            Interlocked.Add(ref _readBytes, (uint)(count - data.Length));
        }
        catch (Exception e)
        {
            _onMessage.OnErrorResume(new ProtocolDeserializeMessageException(Info, message, e));
            return;
        }
        Interlocked.Increment(ref _readMessages);
        try
        {
            _onMessage.OnNext(message);
        }
        catch (Exception e)
        {
            _onMessage.OnErrorResume(new ProtocolPublishMessageException(Info, message, e));
        }

        if (!ignoreReadNotAllData && !data.IsEmpty)
        {
            _onMessage.OnErrorResume(new ProtocolParserReadNotAllDataWhenDeserializePacketException(Info, message));
        }
    }

    #region Dispose

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _onMessage.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        _onMessage.Dispose();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    #endregion
}