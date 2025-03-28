namespace Asv.Common.InvariantParser;

public class IsNullOrWhiteSpaceValidationException() : ValidationException("Value is null or whitespace")
{
    public static IsNullOrWhiteSpaceValidationException Instance { get; } = new();
}