using System;

namespace Asv.IO;

public class ProtocolParserException : Exception
{
    public ProtocolParserInfo Parser { get; }

    public ProtocolParserException(ProtocolParserInfo parser)
    {
        Parser = parser;
    }

    public ProtocolParserException(ProtocolParserInfo parser,string message) : base(message)
    {
        Parser = parser;
    }

    public ProtocolParserException(ProtocolParserInfo parser, string message, Exception inner) : base(message, inner)
    {
        Parser = parser;
    }
}