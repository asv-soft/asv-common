using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Asv.IO;

public interface IProtocolMessageFactory
{
    ProtocolInfo Info { get; }
}

public interface IProtocolMessageFactory<out TProtocolMessageBase, TMessageId>
    : IProtocolMessageFactory
    where TProtocolMessageBase : IProtocolMessage<TMessageId>
    where TMessageId : notnull
{
    TProtocolMessageBase? Create(TMessageId id);
    IEnumerable<TMessageId> GetSupportedIds();
}

public class ProtocolMessageFactory<TProtocolMessageBase, TMessageId>(
    ProtocolInfo info,
    ImmutableDictionary<TMessageId, Func<TProtocolMessageBase>> messageTypes
) : IProtocolMessageFactory<TProtocolMessageBase, TMessageId>
    where TProtocolMessageBase : IProtocolMessage<TMessageId>
    where TMessageId : notnull
{
    public ProtocolInfo Info => info;

    public TProtocolMessageBase? Create(TMessageId id)
    {
        return messageTypes.TryGetValue(id, out var factory) ? factory() : default;
    }

    public IEnumerable<TMessageId> GetSupportedIds() => messageTypes.Keys.Select(x => x);
}
