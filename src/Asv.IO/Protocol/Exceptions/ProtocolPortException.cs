using System.Diagnostics.CodeAnalysis;
using System;

namespace Asv.IO;

[SuppressMessage("Design", "RCS1194:Implement exception constructors", Justification = "Protocol context is required to construct this exception.")]
public class ProtocolPortException : ProtocolException
{
    public IProtocolPort Port { get; }

    public ProtocolPortException(IProtocolPort port)
    {
        Port = port;
    }

    public ProtocolPortException(IProtocolPort port, string message)
        : base(message)
    {
        Port = port;
    }

    public ProtocolPortException(IProtocolPort port, string message, Exception inner)
        : base(message, inner)
    {
        Port = port;
    }
}
