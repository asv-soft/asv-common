using System.Diagnostics.CodeAnalysis;
using System;

namespace Asv.IO;

[SuppressMessage("Design", "RCS1194:Implement exception constructors", Justification = "Protocol context is required to construct this exception.")]
public class ProtocolConnectionException : ProtocolException
{
    public IProtocolConnection Connection { get; }

    public ProtocolConnectionException(IProtocolConnection connection)
    {
        Connection = connection;
    }

    public ProtocolConnectionException(IProtocolConnection connection, string message)
        : base(message)
    {
        Connection = connection;
    }

    public ProtocolConnectionException(
        IProtocolConnection connection,
        string message,
        Exception inner
    )
        : base(message, inner)
    {
        Connection = connection;
    }
}
