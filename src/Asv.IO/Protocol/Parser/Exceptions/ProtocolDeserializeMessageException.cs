using System.Diagnostics.CodeAnalysis;
using System;

namespace Asv.IO;

[SuppressMessage("Design", "RCS1194:Implement exception constructors", Justification = "Protocol context is required to construct this exception.")]
public class ProtocolDeserializeMessageException : ProtocolParserException
{
    public ProtocolDeserializeMessageException(
        ProtocolInfo parser,
        IProtocolMessage message,
        Exception ex
    )
        : this(parser, message.Name, ex) { }

    public ProtocolDeserializeMessageException(
        ProtocolInfo parser,
        string messageName,
        Exception ex
    )
        : base(parser, $"Deserialization {parser}.{messageName} message error.", ex) { }

    public ProtocolDeserializeMessageException(
        ProtocolInfo parser,
        IProtocolMessage message,
        string messageError
    )
        : this(parser, message.Name, messageError) { }

    public ProtocolDeserializeMessageException(
        ProtocolInfo parser,
        string messageName,
        string messageError
    )
        : base(parser, $"Deserialization {parser}.{messageName} message error:{messageError}") { }
}
