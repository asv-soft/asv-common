namespace Asv.Common;

public class NotNumberValidationException()
    : ValidationException("Value is not a number", RS.ValidationException_NaN_Message)
{
    public static NotNumberValidationException Instance { get; } = new();
}
