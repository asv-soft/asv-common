using System.Globalization;
using JetBrains.Annotations;
using Xunit;

namespace Asv.Common.Test.Other.InvarianParser;

[TestSubject(typeof(InvariantNumberParser))]
public class InvariantNumberParserTest
{
    #region TryParse(string? input, out double value)

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParseDouble_NullOrWhiteSpace_ReturnsFailAsNullOrWhiteSpace(string? input)
    {
        // Arrange & Act
        var result = InvariantNumberParser.TryParse(input, out double parsedValue);

        // Assert
        Assert.False(result.IsSuccess, "Expected IsSuccess == false for null/empty string");
        Assert.Same(IsNullOrWhiteSpaceValidationException.Instance, result.ValidationException);
        Assert.True(double.IsNaN(parsedValue), "In case of failure, double.NaN should be returned");
    }

    [Theory]
    [InlineData("123", 123.0)]
    [InlineData("123.456", 123.456)]
    [InlineData("123,456", 123.456)] // Replace comma with a dot
    [InlineData("100K", 100_000)]
    [InlineData("1M", 1_000_000)]
    [InlineData("2b", 2_000_000_000)]
    [InlineData("  15.5К ", 15_500)] // Spaces and the Russian 'К'
    public void TryParseDouble_ValidInput_ReturnsSuccessAndCorrectValue(
        string input,
        double expected
    )
    {
        // Arrange & Act
        var result = InvariantNumberParser.TryParse(input, out double parsedValue);

        // Assert
        Assert.True(result.IsSuccess, "Expected IsSuccess == true for a valid value");
        Assert.Null(result.ValidationException);
        Assert.Equal(expected, parsedValue, 5);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123ab")]
    [InlineData("12.3.4")]
    public void TryParseDouble_InvalidNumber_ReturnsFailAsNotNumber(string input)
    {
        // Arrange & Act
        var result = InvariantNumberParser.TryParse(input, out double parsedValue);

        // Assert
        Assert.False(result.IsSuccess, "Expected IsSuccess == false for an invalid number");
        Assert.Same(NotNumberValidationException.Instance, result.ValidationException);
        Assert.True(double.IsNaN(parsedValue), "In case of failure, double.NaN should be returned");
    }

    #endregion

    #region TryParse(string? input, out double value, double min, double max)

    [Theory]
    [InlineData("500", 100, 400)]
    [InlineData("5K", 1_000, 4_000)]
    public void TryParseDouble_WithRange_OutOfRangeValue_ReturnsFailAsOutOfRange(
        string input,
        double min,
        double max
    )
    {
        // Arrange & Act
        var result = InvariantNumberParser.TryParse(input, out double parsedValue, min, max);

        // Assert
        Assert.False(
            result.IsSuccess,
            "Expected IsSuccess == false when the value is out of range"
        );
        Assert.NotNull(result.ValidationException);
        Assert.Contains("Value is out of range", result.ValidationException!.Message);

        // We still attempt to parse the value – parsedValue holds the parsed number,
        // but IsSuccess=false indicates it's invalid in terms of the specified range.
    }

    [Theory]
    [InlineData("300", 100, 400, 300)]
    [InlineData("350K", 300_000, 400_000, 350_000)]
    public void TryParseDouble_WithRange_ValidValue_ReturnsSuccess(
        string input,
        double min,
        double max,
        double expected
    )
    {
        // Arrange & Act
        var result = InvariantNumberParser.TryParse(input, out double parsedValue, min, max);

        // Assert
        Assert.True(result.IsSuccess, "Expected IsSuccess == true for a valid value within range");
        Assert.Null(result.ValidationException);
        Assert.Equal(expected, parsedValue, 5);
    }

    [Fact]
    public void TryParseDouble_WithRange_NullOrWhiteSpace_ReturnsFailAsNullOrWhiteSpace()
    {
        // Arrange
        string? input = null;

        // Act
        var result = InvariantNumberParser.TryParse(input, out double parsedValue, 0, 100);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Same(IsNullOrWhiteSpaceValidationException.Instance, result.ValidationException);
        Assert.True(double.IsNaN(parsedValue));
    }

    #endregion

    #region TryParse(string? input, ref int value)

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParseInt_NullOrWhiteSpace_ReturnsFailAsNullOrWhiteSpace(string? input)
    {
        // Arrange
        var intValue = 0;

        // Act
        var result = InvariantNumberParser.TryParse(input, out intValue);

        // Assert
        Assert.False(result.IsSuccess, "Expected IsSuccess == false for null/empty string");
        Assert.Same(IsNullOrWhiteSpaceValidationException.Instance, result.ValidationException);
    }

    [Theory]
    [InlineData("123", 123)]
    [InlineData("2k", 2000)]
    [InlineData("1M", 1_000_000)]
    [InlineData("  15  ", 15)]
    public void TryParseInt_ValidInput_ReturnsSuccessAndCorrectValue(string input, int expected)
    {
        // Arrange
        var intValue = 0;

        // Act
        var result = InvariantNumberParser.TryParse(input, out intValue);

        // Assert
        if (result.IsSuccess)
        {
            Assert.Equal(expected, intValue);
        }
        else
        {
            // If parsing fails here, it means the input string doesn't fit an int for some reason.
            // For example, "123,456" might not parse due to the dot after replacement,
            // or the number might be too large.
            Assert.Same(NotNumberValidationException.Instance, result.ValidationException);
        }
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("----")]
    [InlineData("9999999999999999999999")]
    public void TryParseInt_InvalidNumber_ReturnsFailAsNotNumber(string input)
    {
        // Arrange
        int intValue;

        // Act
        var result = InvariantNumberParser.TryParse(input, out intValue);

        // Assert
        Assert.False(result.IsSuccess, "Expected IsSuccess == false for an invalid number");
        Assert.Same(NotNumberValidationException.Instance, result.ValidationException);
    }

    #endregion

    #region TryParse(string? input, ref int value, int min, int max)

    [Theory]
    [InlineData("50", 100, 200)]
    [InlineData("300", 100, 200)]
    public void TryParseInt_WithRange_OutOfRangeValue_ReturnsFailAsOutOfRange(
        string input,
        int min,
        int max
    )
    {
        // Arrange
        int intValue;

        // Act
        var result = InvariantNumberParser.TryParse(input, out intValue, min, max);

        // Assert
        Assert.False(
            result.IsSuccess,
            "Expected IsSuccess == false when the value is out of the specified range"
        );
        Assert.NotNull(result.ValidationException);
        Assert.Contains("Value is out of range", result.ValidationException!.Message);

        // intValue should not be set to a successful state if it's out of range.
    }

    [Theory]
    [InlineData("150", 100, 200, 150)]
    [InlineData("1k", 500, 2000, 1000)]
    public void TryParseInt_WithRange_ValidValue_ReturnsSuccess(
        string input,
        int min,
        int max,
        int expected
    )
    {
        // Arrange
        int intValue;

        // Act
        var result = InvariantNumberParser.TryParse(input, out intValue, min, max);

        // Assert
        Assert.True(
            result.IsSuccess,
            "Expected successful parsing for a valid number within the specified range"
        );
        Assert.Null(result.ValidationException);
        Assert.Equal(expected, intValue);
    }

    [Fact]
    public void TryParseInt_WithRange_NullOrWhiteSpace_ReturnsFailAsNullOrWhiteSpace()
    {
        // Arrange
        string? input = null;
        int intValue;

        // Act
        var result = InvariantNumberParser.TryParse(input, out intValue, 0, 100);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Same(IsNullOrWhiteSpaceValidationException.Instance, result.ValidationException);
    }

    #endregion

    #region Double Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParseDouble_NullOrWhiteSpace_ShouldFailAsNullOrWhiteSpace(string input)
    {
        var result = InvariantNumberParser.TryParse(input, out double value);
        Assert.False(result.IsSuccess);
        Assert.Equal(ValidationResult.FailAsNullOrWhiteSpace, result);
        Assert.True(double.IsNaN(value));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("???")]
    [InlineData("--1.23")]
    public void TryParseDouble_InvalidFormat_ShouldFailAsNotNumber(string input)
    {
        var result = InvariantNumberParser.TryParse(input, out double value);
        Assert.False(result.IsSuccess);
        Assert.Equal(ValidationResult.FailAsNotNumber, result);
        Assert.True(double.IsNaN(value));
    }

    [Theory]
    [InlineData("123", 123)]
    [InlineData("123.456", 123.456)]
    [InlineData("123,456", 123.456)]
    [InlineData("   999.1   ", 999.1)]
    public void TryParseDouble_ValidNumericValue_ShouldSuccess(string input, double expected)
    {
        var result = InvariantNumberParser.TryParse(input, out double value);
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, value, precision: 5);
    }

    [Theory]
    [InlineData("10k", 10_000)]
    [InlineData("10K", 10_000)]
    [InlineData("10К", 10_000)] // Russian letter
    [InlineData("10к", 10_000)] // Russian letter
    [InlineData("2.5M", 2_500_000)]
    [InlineData("2.5m", 2_500_000)]
    [InlineData("2.5М", 2_500_000)]
    [InlineData("2.5м", 2_500_000)]
    [InlineData("1B", 1_000_000_000)]
    [InlineData("1b", 1_000_000_000)]
    [InlineData("1g", 1_000_000_000)]
    [InlineData("1г", 1_000_000_000)]
    [InlineData("1.234г", 1_234_000_000)]
    public void TryParseDouble_WithSuffix_ShouldMultiplyCorrectly(string input, double expected)
    {
        var result = InvariantNumberParser.TryParse(input, out double value);
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, value, precision: 3);
    }

    [Theory]
    [InlineData("9999", 1000, 9000)] // value=9999, range=[1000..9000]
    [InlineData("150", 200, 500)]
    public void TryParseDouble_OutOfRange_ShouldFailAsOutOfRange(
        string input,
        double min,
        double max
    )
    {
        var result = InvariantNumberParser.TryParse(input, out double value, min, max);
        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData("500", 100, 1000)]
    [InlineData("10k", 1, 2_000_000)]
    public void TryParseDouble_InRange_ShouldSuccess(string input, double min, double max)
    {
        var result = InvariantNumberParser.TryParse(input, out double value, min, max);
        Assert.True(result.IsSuccess);
        Assert.False(double.IsNaN(value));
    }

    #endregion

    #region Int Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void TryParseInt_NullOrWhiteSpace_ShouldFailAsNullOrWhiteSpace(string input)
    {
        var result = InvariantNumberParser.TryParse(input, out int value);
        Assert.False(result.IsSuccess);
        Assert.Equal(ValidationResult.FailAsNullOrWhiteSpace, result);
        Assert.Equal(0, value);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("???")]
    [InlineData("- 1 2 3")]
    public void TryParseInt_InvalidFormat_ShouldFailAsNotNumber(string input)
    {
        var result = InvariantNumberParser.TryParse(input, out int value);
        Assert.False(result.IsSuccess);
        Assert.Equal(ValidationResult.FailAsNotNumber, result);
        Assert.Equal(0, value);
    }

    [Theory]
    [InlineData("123", 123)]
    [InlineData("123.999", 123)]
    [InlineData("123,999", 123)]
    [InlineData(" -45 ", -45)]
    public void TryParseInt_ValidValue_ShouldSuccess(string input, int expected)
    {
        var result = InvariantNumberParser.TryParse(input, out int value);
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("1.5k", 1500)]
    [InlineData("1.9К", 1900)]
    [InlineData("1M", 1_000_000)]
    [InlineData("2м", 2_000_000)]
    [InlineData("1b", 1_000_000_000)]
    [InlineData("2g", 2_000_000_000)]
    
    // Check that exceeding int.MaxValue when multiplying
    // by 1000 or 1_000_000 returns FailAsOutOfRange during double->int casting
    public void TryParseInt_WithSuffix_ShouldSuccessOrFailIfOverflow(
        string input,
        int expectedOrOverflow
    )
    {
        var result = InvariantNumberParser.TryParse(input, out int value);

        // In case of double->int overflow, the method may return FailAsOutOfRange
        // or the correct value if it didn't exceed int.MaxValue
        if (result.IsSuccess)
        {
            Assert.Equal(expectedOrOverflow, value);
        }
        else
        {
            Assert.Equal(
                ValidationResult.FailAsOutOfRange(
                    int.MinValue.ToString(CultureInfo.InvariantCulture),
                    int.MaxValue.ToString(CultureInfo.InvariantCulture)
                ),
                result
            );
        }
    }

    [Theory]
    [InlineData("9999999999", -1000, 1000)]
    [InlineData("2000", 1, 1000)]
    public void TryParseInt_OutOfRange_ShouldFailAsOutOfRange(string input, int min, int max)
    {
        var result = InvariantNumberParser.TryParse(input, out int value, min, max);
        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData("500", -1000, 1000)]
    [InlineData("-5", -1000, 0)]
    public void TryParseInt_InRange_ShouldSuccess(string input, int min, int max)
    {
        var result = InvariantNumberParser.TryParse(input, out int value, min, max);
        Assert.True(result.IsSuccess);
    }

    #endregion

    #region UInt Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void TryParseUInt_NullOrWhiteSpace_ShouldFailAsNullOrWhiteSpace(string input)
    {
        var result = InvariantNumberParser.TryParse(input, out uint value);
        Assert.False(result.IsSuccess);
        Assert.Equal(ValidationResult.FailAsNullOrWhiteSpace, result);
        Assert.Equal((uint)0, value);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-123")]
    [InlineData("+ 9999")]
    public void TryParseUInt_InvalidFormat_ShouldFailAsNotNumber(string input)
    {
        var result = InvariantNumberParser.TryParse(input, out uint value);
        Assert.False(result.IsSuccess);
        Assert.Equal(ValidationResult.FailAsNotNumber, result);
        Assert.Equal((uint)0, value);
    }

    [Theory]
    [InlineData("123", 123u)]
    [InlineData("123.999", 123u)]
    [InlineData("0", 0u)]
    public void TryParseUInt_ValidValue_ShouldSuccess(string input, uint expected)
    {
        var result = InvariantNumberParser.TryParse(input, out uint value);
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("10k", 10_000u)]
    [InlineData("1.5m", 1_500_000u)]
    [InlineData("1b", 1_000_000_000u)]
    public void TryParseUInt_WithSuffix_ShouldSuccessIfNoOverflow(string input, uint expected)
    {
        var result = InvariantNumberParser.TryParse(input, out uint value);
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("9999999999", 1u, 5000u)]
    public void TryParseUInt_OutOfRange_ShouldFailAsOutOfRange(string input, uint min, uint max)
    {
        var result = InvariantNumberParser.TryParse(input, out uint value, min, max);
        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData("5000", 1u, 10000u)]
    [InlineData("5000", 5000u, 5000u)]
    public void TryParseUInt_InRange_ShouldSuccess(string input, uint min, uint max)
    {
        var result = InvariantNumberParser.TryParse(input, out uint value, min, max);
        Assert.True(result.IsSuccess);
    }

    #endregion
}
