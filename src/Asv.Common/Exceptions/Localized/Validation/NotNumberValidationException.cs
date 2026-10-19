using System;

namespace Asv.Common;

public class NotNumberValidationException : ValidationException
{
    public NotNumberValidationException()
        : base("Value is not a number", RS.ValidationException_NaN_Message) { }

    public NotNumberValidationException(string? message, string? localizedMessage = null)
        : base(message, localizedMessage) { }

    public NotNumberValidationException(
        string? message,
        Exception? innerException,
        string? localizedMessage = null
    )
        : base(message, innerException, localizedMessage) { }

    public static NotNumberValidationException Instance { get; } = new();
}
