using System;
using System.Collections.Immutable;

namespace Asv.IO;

public interface IProtocolMessageFactoryBuilder<TProtocolMessageBase, TMessageId>
    where TProtocolMessageBase : IProtocolMessage<TMessageId> where TMessageId : notnull
{
    ProtocolMessageFactoryBuilder<TProtocolMessageBase, TMessageId> Add<TMessage>()
        where TMessage : TProtocolMessageBase, new();
    bool Remove(TMessageId id);
    void Clear();
}

public class ProtocolMessageFactoryBuilder<TProtocolMessageBase, TMessageId>(ProtocolInfo info)
    : IProtocolMessageFactoryBuilder<TProtocolMessageBase, TMessageId>
    where TProtocolMessageBase : IProtocolMessage<TMessageId>
    where TMessageId : notnull
{
    private readonly ImmutableDictionary<TMessageId, Func<TProtocolMessageBase>>.Builder _builder
        = ImmutableDictionary.CreateBuilder<TMessageId, Func<TProtocolMessageBase>>();

    public ProtocolMessageFactoryBuilder<TProtocolMessageBase, TMessageId> Add<TMessage>(TMessageId id)
        where TMessage : TProtocolMessageBase, new()
    {
        _builder.Add(id, () => new TMessage());
        return this;
    }

    public ProtocolMessageFactoryBuilder<TProtocolMessageBase, TMessageId> Add<TMessage>() where TMessage : TProtocolMessageBase, new()
    {
        var temp = new TMessage();
        _builder.Add(temp.Id, () => new TMessage());
        return this;
    }

    public bool Remove(TMessageId id)
    {
        return _builder.Remove(id);
    }

    public void Clear()
    {
        _builder.Clear();
    }

    public IProtocolMessageFactory<TProtocolMessageBase, TMessageId> Build()
    {
        return new ProtocolMessageFactory<TProtocolMessageBase, TMessageId>(info, _builder.ToImmutable());
    }
}