using System.Diagnostics.CodeAnalysis;
using System;

namespace Asv.IO;

[SuppressMessage("Design", "RCS1194:Implement exception constructors", Justification = "Protocol context is required to construct this exception.")]
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
