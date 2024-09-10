using System;

namespace Asv.IO;

public class ULogException : Exception
{
    public ULogException()
    {
    }

    public ULogException(string message) : base(message)
    {
    }

    public ULogException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class ULogSizeTooSmallException(string section) 
    : ULogException($"Size too small to read {section}");
    
