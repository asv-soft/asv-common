namespace Asv.Common.InvariantParser;

public readonly struct ValidationResult
{
    
    public required bool IsSuccess { get; init; }
    public ValidationException? ValidationException { get; init; }
    
    
    public static ValidationResult Success { get; } = new() { IsSuccess = true };

    public static ValidationResult FailAsNullOrWhiteSpace { get; } = new()
    {
        IsSuccess = false, 
        ValidationException = IsNullOrWhiteSpaceValidationException.Instance
    };
    
    public static ValidationResult FailAsNotNumber = new()
    {
        IsSuccess = false, 
        ValidationException = NotNumberValidationException.Instance
    };

    public static ValidationResult FailAsOutOfRange(string min, string max)
    {
        return new ValidationResult
        {
            IsSuccess = false,
            ValidationException = new ValidationException($"Value is out of range. Min: {min}, Max: {max}")
        };
    }
}