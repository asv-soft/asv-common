using System;

namespace Asv.Common;

public class InvalidCharactersValidationException : ValidationException
{
    public InvalidCharactersValidationException()
        : base("Value contains invalid characters", RS.ValidationException_InvalidCharacters_Message) { }

    public InvalidCharactersValidationException(string? message, string? localizedMessage = null)
        : base(message, localizedMessage) { }

    public InvalidCharactersValidationException(
        string? message,
        Exception? innerException,
        string? localizedMessage = null
    )
        : base(message, innerException, localizedMessage) { }

    public static InvalidCharactersValidationException Instance { get; } = new();
}
