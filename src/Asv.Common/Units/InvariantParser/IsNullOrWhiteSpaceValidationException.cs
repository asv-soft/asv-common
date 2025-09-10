namespace Asv.Common;

public class IsNullOrWhiteSpaceValidationException()
    : ValidationException("Value is null or whitespace")
{
    public static IsNullOrWhiteSpaceValidationException Instance { get; } = new();
}
