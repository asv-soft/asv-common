using System;

namespace Asv.Common;

public sealed class UnknownValidationException : ValidationException
{
    public UnknownValidationException()
        : base("Unknown validation exception", RS.ValidationException_Unknown_Message) { }

    public UnknownValidationException(string? message, string? localizedMessage = null)
        : base(message, localizedMessage) { }

    public UnknownValidationException(
        string? message,
        Exception? innerException,
        string? localizedMessage = null
    )
        : base(message, innerException, localizedMessage) { }
}
