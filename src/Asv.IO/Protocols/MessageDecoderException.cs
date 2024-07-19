using System;

namespace Asv.IO;

public class ProtocolException : Exception
{
    public ProtocolException()
    {
    }

    public ProtocolException(string message) : base(message)
    {
    }

    public ProtocolException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class MessageDecoderException : ProtocolException
{
    public string ProtocolId { get; }

    public MessageDecoderException(string protocolId)
    {
        ProtocolId = protocolId;
    }

    public MessageDecoderException(string protocolId,string message) : base(message)
    {
    }

    public MessageDecoderException(string protocolId,string message, Exception inner) : base(message, inner)
    {
    }
}