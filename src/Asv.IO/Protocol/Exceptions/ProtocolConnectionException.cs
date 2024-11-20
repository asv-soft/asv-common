using System;

namespace Asv.IO;

public class ProtocolConnectionException : ProtocolException
{
    public IProtocolEndpoint Endpoint { get; }

    public ProtocolConnectionException(IProtocolEndpoint endpoint)
    {
        Endpoint = endpoint;
    }

    public ProtocolConnectionException(IProtocolEndpoint endpoint, string message) : base(message)
    {
        Endpoint = endpoint;
    }

    public ProtocolConnectionException(IProtocolEndpoint endpoint, string message, Exception inner) : base(message, inner)
    {
        Endpoint = endpoint;
    }
}