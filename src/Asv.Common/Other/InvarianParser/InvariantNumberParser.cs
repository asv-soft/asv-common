using System;
using System.Globalization;
using System.Linq;

namespace Asv.Common.InvarianParser;

public static class InvariantNumberParser
{
    public static readonly char[] Kilo = ['K', 'k', 'К', 'k'];
    public static readonly char[] Mega = ['M', 'm', 'М', 'м'];
    public static readonly char[] Giga = ['B', 'b', 'G', 'g', 'Г', 'г'];
    public static readonly char[] TrimToEmpty = [' ','_'];
    
    private static ValidationResult ValidateNullOrWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? ValidationResult.FailAsNullOrWhiteSpace : ValidationResult.Success;
    }
    
    private static ValidationResult ValidateMultiply(ref ReadOnlySpan<char> value, out int multiply)
    {
        multiply = 1;
        value = value.Trim(TrimToEmpty);
        if (value.IsEmpty)
        {
            return ValidationResult.FailAsNullOrWhiteSpace;
        }

        var lastChar = value[^1];
        if (Kilo.Contains(lastChar))
        {
            multiply = 1_000;
            value = value[..^1];
        }
        else if (Mega.Contains(lastChar))
        {
            multiply = 1_000_000;
            value = value[..^1];
        }
        else if (Giga.Contains(lastChar))
        {
            multiply = 1_000_000_000;
            value = value[..^1];
        }
        
        return value.IsEmpty ? ValidationResult.FailAsNullOrWhiteSpace : ValidationResult.Success;
    }

    public static ValidationResult TryParse(string? input, out double value, double min, double max)
    {
        value = double.NaN;
        var result = TryParse(input, out value);
        if (result.IsSuccess == false) return result;
        if (min > value || value > max)
        {
            return ValidationResult.FailAsOutOfRange(min.ToString(CultureInfo.InvariantCulture), max.ToString(CultureInfo.InvariantCulture));
        }
        return ValidationResult.Success;
    }

    public static ValidationResult TryParse(string? input, out double value)
    {
        value = double.NaN;
        var result = ValidateNullOrWhiteSpace(input);
        if (result.IsSuccess == false) return result;
        var span = input.AsSpan();
        result = ValidateMultiply(ref span, out var multiply);
        if (result.IsSuccess == false) return result;
        Span<char> editValue = stackalloc char[span.Length];
        span.Replace(editValue,',','.');
        if (double.TryParse(editValue, NumberStyles.Any, CultureInfo.InvariantCulture, out value) == false)
        {
            value = double.NaN;
            return ValidationResult.FailAsNotNumber;
        }
        value *= multiply;
        return ValidationResult.Success;
    }

    public static ValidationResult TryParse(string? input, out int value, int min, int max)
    {
        value = 0;
        var result = TryParse(input, out value);
        if (result.IsSuccess == false) return result;
        if (min > value || value > max)
        {
            return ValidationResult.FailAsOutOfRange(min.ToString(CultureInfo.InvariantCulture), max.ToString(CultureInfo.InvariantCulture));
        }
        return ValidationResult.Success;
    }
    
    public static ValidationResult TryParse(string? input, out int value)
    {
        value = 0;
        var result = ValidateNullOrWhiteSpace(input);
        if (result.IsSuccess == false) return result;
        var span = input.AsSpan();
        result = ValidateMultiply(ref span, out var multiply);
        if (result.IsSuccess == false) return result;
        Span<char> editValue = stackalloc char[span.Length];
        span.Replace(editValue,',','.');
        if (int.TryParse(editValue, NumberStyles.Any, CultureInfo.InvariantCulture, out value) == false)
        {
            
            return ValidationResult.FailAsNotNumber;
        }
        value *= multiply;
        return ValidationResult.Success;
    }
    
    
    
}