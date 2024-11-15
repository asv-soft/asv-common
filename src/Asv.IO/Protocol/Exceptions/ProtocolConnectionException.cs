using System;

namespace Asv.IO;

public class ProtocolConnectionException : ProtocolException
{
    public IProtocolConnection Connection { get; }

    public ProtocolConnectionException(IProtocolConnection connection)
    {
        Connection = connection;
    }

    public ProtocolConnectionException(IProtocolConnection connection, string message) : base(message)
    {
        Connection = connection;
    }

    public ProtocolConnectionException(IProtocolConnection connection, string message, Exception inner) : base(message, inner)
    {
        Connection = connection;
    }
}