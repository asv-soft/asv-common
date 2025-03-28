namespace Asv.Common.InvariantParser;

public class NotNumberValidationException() : ValidationException("Value is not number")
{
    public static NotNumberValidationException Instance { get; } = new();
}