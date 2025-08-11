using System.Threading.Tasks;

namespace Asv.Common;

public readonly struct ValidationResult
{
    public required bool IsSuccess { get; init; }
    public bool IsFailed => !IsSuccess;
    public ValidationException? ValidationException { get; init; }
    
    public static implicit operator ValidationResult(ValidationException exc)
    {
        return new ValidationResult { IsSuccess = false, ValidationException = exc };
    }

    public static implicit operator ValueTask<ValidationResult>(ValidationResult result)
    {
        return ValueTask.FromResult(result);
    }
    public static ValidationResult Success { get; } = new() { IsSuccess = true };

    public static ValidationResult FailAsNullOrWhiteSpace { get; } = new()
    {
        IsSuccess = false, 
        ValidationException = IsNullOrWhiteSpaceValidationException.Instance
    };
    
    public static ValidationResult FailAsInvalidCharacters { get; } = new()
    {
        IsSuccess = false, 
        ValidationException = InvalidCharactersValidationException.Instance,
    };
    
    public static ValidationResult FailAsNotNumber = new()
    {
        IsSuccess = false, 
        ValidationException = NotNumberValidationException.Instance
    };

    public static ValidationResult FailAsOutOfRange(string min, string max) 
        => FailFromErrorMessage("Value is out of range. Min: {min}, Max: {max}");

    public static ValidationResult FailFromErrorMessage(string errorMessage)
    {
        return new ValidationResult
        {
            IsSuccess = false,
            ValidationException = new ValidationException(errorMessage)
        };
    }
}