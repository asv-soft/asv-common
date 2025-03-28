namespace Asv.Common.InvarianParser;

public class NotNumberValidationException() : ValidationException("Value is not number")
{
    public static NotNumberValidationException Instance { get; } = new();
}