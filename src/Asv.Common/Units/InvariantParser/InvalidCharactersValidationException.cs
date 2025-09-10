namespace Asv.Common;

public class InvalidCharactersValidationException()
    : ValidationException("Value contain invalid characters")
{
    public static InvalidCharactersValidationException Instance { get; } = new();
}
