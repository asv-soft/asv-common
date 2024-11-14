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

public class ProtocolConnectionException : ProtocolException
{
    public ProtocolConnectionException(IProtocolConnection connection)
    {
        
    }

    public ProtocolConnectionException(IProtocolConnection connection, string message) : base(message)
    {
    }

    public ProtocolConnectionException(IProtocolConnection connection, string message, Exception inner) : base(message, inner)
    {
    }
}