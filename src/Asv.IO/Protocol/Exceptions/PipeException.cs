using System;

namespace Asv.IO;

public class PipeException : Exception
{
    public PipeException()
    {
    }

    public PipeException(string message) : base(message)
    {
    }

    public PipeException(string message, Exception inner) : base(message, inner)
    {
    }
}