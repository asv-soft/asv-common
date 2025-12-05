using System;

namespace Asv.Common;

public class ValidationException : LocalizedException
{
    public ValidationException()
        : base() { }

    public ValidationException(string? message, string? localizedMessage = null)
        : base(message, localizedMessage) { }

    public ValidationException(
        string? message,
        Exception? innerException,
        string? localizedMessage = null
    )
        : base(message, innerException, localizedMessage) { }
}
