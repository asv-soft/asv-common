using System;

namespace Asv.IO;

public class ProtocolPortException : ProtocolException
{
    public IProtocolPort Port { get; }

    public ProtocolPortException(IProtocolPort port)
    {
        Port = port;
    }

    public ProtocolPortException(IProtocolPort port, string message) : base(message)
    {
        Port = port;
    }

    public ProtocolPortException(IProtocolPort port, string message, Exception inner) : base(message, inner)
    {
        Port = port;
    }
}