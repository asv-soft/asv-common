using System;

namespace Asv.IO;

public class ProtocolPublishMessageException : ProtocolParserException
{
    public ProtocolPublishMessageException(
        ProtocolInfo parser,
        IProtocolMessage message,
        Exception ex
    )
        : base(
            parser,
            $"Publication {parser}.{message.Name}[ID={message.GetIdAsString()}] throw exception:{ex.Message}",
            ex
        ) { }
}
