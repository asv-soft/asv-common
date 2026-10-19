using System.Diagnostics.CodeAnalysis;
using System;

namespace Asv.IO;

[SuppressMessage("Design", "RCS1194:Implement exception constructors", Justification = "Protocol context is required to construct this exception.")]
public class ProtocolParserException : Exception
{
    public ProtocolInfo Parser { get; }

    public ProtocolParserException(ProtocolInfo parser)
    {
        Parser = parser;
    }

    public ProtocolParserException(ProtocolInfo parser, string message)
        : base(message)
    {
        Parser = parser;
    }

    public ProtocolParserException(ProtocolInfo parser, string message, Exception inner)
        : base(message, inner)
    {
        Parser = parser;
    }
}
