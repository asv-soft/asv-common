using System;

namespace Asv.Common;

public class IsNullOrWhiteSpaceValidationException : ValidationException
{
    public IsNullOrWhiteSpaceValidationException()
        : base("Value is null or whitespace", RS.ValidationException_IsNullOrWhiteSpace_Message) { }

    public IsNullOrWhiteSpaceValidationException(string? message, string? localizedMessage = null)
        : base(message, localizedMessage) { }

    public IsNullOrWhiteSpaceValidationException(
        string? message,
        Exception? innerException,
        string? localizedMessage = null
    )
        : base(message, innerException, localizedMessage) { }

    public static IsNullOrWhiteSpaceValidationException Instance { get; } = new();
}
