using System;

namespace Asv.IO;

public class ProtocolParserException : Exception
{
    public ProtocolInfo Parser { get; }

    public ProtocolParserException(ProtocolInfo parser)
    {
        Parser = parser;
    }

    public ProtocolParserException(ProtocolInfo parser,string message) : base(message)
    {
        Parser = parser;
    }

    public ProtocolParserException(ProtocolInfo parser, string message, Exception inner) : base(message, inner)
    {
        Parser = parser;
    }
}