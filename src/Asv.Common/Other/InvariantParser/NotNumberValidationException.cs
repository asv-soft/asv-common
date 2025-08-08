namespace Asv.Common;

public class NotNumberValidationException() : ValidationException("Value is not number")
{
    public static NotNumberValidationException Instance { get; } = new();
}