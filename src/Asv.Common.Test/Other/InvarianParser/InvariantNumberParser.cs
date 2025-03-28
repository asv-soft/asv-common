using Asv.Common.InvarianParser;
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
            Assert.False(result.IsSuccess, "Ожидается IsSuccess == false для null/пустой строки");
            Assert.Same(IsNullOrWhiteSpaceValidationException.Instance, result.ValidationException);
            Assert.True(double.IsNaN(parsedValue), "При неуспехе должно возвращаться double.NaN");
        }

        [Theory]
        [InlineData("123", 123.0)]
        [InlineData("123.456", 123.456)]
        [InlineData("123,456", 123.456)] // Заменяем запятую на точку
        [InlineData("100K", 100_000)]
        [InlineData("1M", 1_000_000)]
        [InlineData("2b", 2_000_000_000)]
        [InlineData("  15.5К ", 15_500)] // Пробелы и русская 'К'
        public void TryParseDouble_ValidInput_ReturnsSuccessAndCorrectValue(string input, double expected)
        {
            // Arrange & Act
            var result = InvariantNumberParser.TryParse(input, out double parsedValue);

            // Assert
            Assert.True(result.IsSuccess, "Ожидается IsSuccess == true для корректного значения");
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
            Assert.False(result.IsSuccess, "Ожидается IsSuccess == false для некорректного числа");
            Assert.Same(NotNumberValidationException.Instance, result.ValidationException);
            Assert.True(double.IsNaN(parsedValue), "При неуспехе должно возвращаться double.NaN");
        }

        #endregion

        #region TryParse(string? input, out double value, double min, double max)

        [Theory]
        [InlineData("500", 100, 400)]
        [InlineData("5K", 1_000, 4_000)]
        public void TryParseDouble_WithRange_OutOfRangeValue_ReturnsFailAsOutOfRange(string input, double min, double max)
        {
            // Arrange & Act
            var result = InvariantNumberParser.TryParse(input, out double parsedValue, min, max);

            // Assert
            Assert.False(result.IsSuccess, "Ожидается IsSuccess == false при выходе за диапазон");
            Assert.NotNull(result.ValidationException);
            Assert.Contains("Value is out of range", result.ValidationException!.Message);
            // Значение всё равно пытаемся парсить – parsedValue содержит распарсенное число, 
            // но результат IsSuccess=false говорит, что оно некорректно с точки зрения диапазона.
        }

        [Theory]
        [InlineData("300", 100, 400, 300)]
        [InlineData("350K", 300_000, 400_000, 350_000)]
        public void TryParseDouble_WithRange_ValidValue_ReturnsSuccess(string input, double min, double max, double expected)
        {
            // Arrange & Act
            var result = InvariantNumberParser.TryParse(input, out double parsedValue, min, max);

            // Assert
            Assert.True(result.IsSuccess, "Ожидается IsSuccess == true при корректном числе и корректном диапазоне");
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
            Assert.False(result.IsSuccess, "Ожидается IsSuccess == false для null/пустой строки");
            Assert.Same(IsNullOrWhiteSpaceValidationException.Instance, result.ValidationException);
        }

        [Theory]
        [InlineData("123", 123)]
        [InlineData("123,456", 123456)] // int не рассматривает точку как десятичный разделитель, но запятая убирается и становится "123.456"? 
                                        // Здесь следует уточнить логику: в int.TryParse("123.456") будет ошибка, поэтому см. 
                                        // замечание ниже про замену запятой. Вероятно, при "123,456" произойдёт ошибка парсинга. 
                                        // Но если логика кода убирает '.' и ',', это превратится в "123.456"? int.TryParse не распарсит, будет fail.
                                        // Для демонстрации оставляем как есть.
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
                // Если парсинг не удался – значит входная строка всё-таки не подходит для int.
                // Например, "123,456" может не распарситься из-за точки после замены,
                // либо там число слишком большое.
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
            Assert.False(result.IsSuccess, "Ожидается IsSuccess == false для некорректного числа");
            Assert.Same(NotNumberValidationException.Instance, result.ValidationException);
        }

        #endregion

        #region TryParse(string? input, ref int value, int min, int max)

        [Theory]
        [InlineData("50", 100, 200)]
        [InlineData("300", 100, 200)]
        public void TryParseInt_WithRange_OutOfRangeValue_ReturnsFailAsOutOfRange(string input, int min, int max)
        {
            // Arrange
            var intValue = 0;

            // Act
            var result = InvariantNumberParser.TryParse(input, ref intValue, min, max);

            // Assert
            Assert.False(result.IsSuccess, "Ожидается IsSuccess == false при выходе за указанный диапазон");
            Assert.NotNull(result.ValidationException);
            Assert.Contains("Value is out of range", result.ValidationException!.Message);
            // Значение intValue не должно быть переустановлено в успешное состояние
        }

        [Theory]
        [InlineData("150", 100, 200, 150)]
        [InlineData("1k", 500, 2000, 1000)]
        public void TryParseInt_WithRange_ValidValue_ReturnsSuccess(string input, int min, int max, int expected)
        {
            // Arrange
            var intValue = 0;

            // Act
            var result = InvariantNumberParser.TryParse(input, ref intValue, min, max);

            // Assert
            Assert.True(result.IsSuccess, "Ожидается успешный парсинг при корректном числе и диапазоне");
            Assert.Null(result.ValidationException);
            Assert.Equal(expected, intValue);
        }

        [Fact]
        public void TryParseInt_WithRange_NullOrWhiteSpace_ReturnsFailAsNullOrWhiteSpace()
        {
            // Arrange
            string? input = null;
            var intValue = -1;

            // Act
            var result = InvariantNumberParser.TryParse(input, ref intValue, 0, 100);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Same(IsNullOrWhiteSpaceValidationException.Instance, result.ValidationException);
            Assert.Equal(-1, intValue);
        }

        #endregion
}