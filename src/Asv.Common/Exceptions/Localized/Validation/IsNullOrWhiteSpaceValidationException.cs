namespace Asv.Common;

public class IsNullOrWhiteSpaceValidationException()
    : ValidationException(
        "Value is null or whitespace",
        RS.ValidationException_IsNullOrWhiteSpace_Message
    )
{
    public static IsNullOrWhiteSpaceValidationException Instance { get; } = new();
}
