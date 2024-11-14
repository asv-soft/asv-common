using System;

namespace Asv.IO;

public class PipePortException : PipeException
{
    public IPipePort Port { get; }

    public PipePortException(IPipePort port)
    {
        Port = port;
    }

    public PipePortException(string message, IPipePort port) : base(message)
    {
        Port = port;
    }

    public PipePortException(string message, Exception inner, IPipePort port) : base(message, inner)
    {
        Port = port;
    }
}