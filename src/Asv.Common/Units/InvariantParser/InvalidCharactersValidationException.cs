namespace Asv.Common;

public class InvalidCharactersValidationException()
    : ValidationException(
        "Value contains invalid characters",
        RS.ValidationException_InvalidCharacters_Message
    )
{
    public static InvalidCharactersValidationException Instance { get; } = new();
}
