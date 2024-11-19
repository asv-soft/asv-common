using System;
using System.Collections.Generic;

namespace Asv.IO;

public class ProtocolInfo(string id,string name)
{
    public string Id { get; } = id;
    public string Name { get; } = name;

    public override string ToString()
    {
        return $"{Name}[{Id}]";
    }
}

public interface IProtocolMessageFactory
{
    ProtocolInfo Info { get; }
}

public interface IProtocolMessageFactory<out TMessage, in TMessageId> : IProtocolMessageFactory
    where TMessage : IProtocolMessage<TMessageId>
    where TMessageId : notnull
{
    TMessage? Create(TMessageId id);
    
}
