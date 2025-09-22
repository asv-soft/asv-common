using System.Globalization;
using System.Threading;
using Xunit;

namespace Asv.Common.Test.Other;

public class AngleDmTest
{
    [Theory]
    [InlineData("2.40", 2.40, "en-US")]
    [InlineData("-3.40", -3.40, "en-US")]
    [InlineData("-0.410", -0.41, "en-US")]
    [InlineData("0.410", 0.41, "en-US")]
    [InlineData("2,40", 2.40, "ru-RU")] // Примеры для русской локали
    [InlineData("-3,40", -3.40, "ru-RU")]
    [InlineData("-0,410", -0.41, "ru-RU")]
    [InlineData("0,410", 0.41, "ru-RU")]
    public void CheckDoubleValues(string input, double expectedValue, string culture)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.CurrentCulture = new CultureInfo(culture);

        // Проверка парсинга с учетом локали
        Assert.True(AngleDm.TryParse(input, out var value));
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [InlineData("-0 0", 0)]
    [InlineData("+0 0", 0)]
    [InlineData(" -0 0", 0)]
    [InlineData(" +0 0", 0)]
    [InlineData(" 0 0", 0)]
    [InlineData("0 0 ", 0)]
    public void CheckPlusAndMinus(string input, double expectedValue)
    {
        Assert.True(AngleDm.TryParse(input, out var value));
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [InlineData(@"0° 0'", 0, "en-US")]
    [InlineData(@"0˚ 0'", 0, "en-US")]
    [InlineData(@"0º 0'", 0, "en-US")]
    [InlineData(@"0^ 0'", 0, "en-US")]
    [InlineData(@"0~ 0'", 0, "en-US")]
    [InlineData(@"0* 0'", 0, "en-US")]
    [InlineData(@"0 0'", 0, "en-US")]
    [InlineData(@"0° 0'", 0, "ru-RU")]
    [InlineData(@"0˚ 0'", 0, "ru-RU")]
    [InlineData(@"0º 0'", 0, "ru-RU")]
    [InlineData(@"0^ 0'", 0, "ru-RU")]
    [InlineData(@"0~ 0'", 0, "ru-RU")]
    [InlineData(@"0* 0'", 0, "ru-RU")]
    [InlineData(@"0 0'", 0, "ru-RU")]
    public void CheckDegreeSymbols(string input, double expectedValue, string culture)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.CurrentCulture = new CultureInfo(culture);

        // Проверка парсинга для различных символов градусов с учетом локали
        Assert.True(AngleDm.TryParse(input, out var value));
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [InlineData(@"000° 00'", 0, "en-US")]
    [InlineData(@"000° 00′", 0, "en-US")]
    [InlineData(@"000° 00'", 0, "ru-RU")]
    [InlineData(@"000° 00′", 0, "ru-RU")]
    public void CheckMinuteSymbols(string input, double expectedValue, string culture)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.CurrentCulture = new CultureInfo(culture);

        // Проверка парсинга для различных символов минут с учетом локали
        Assert.True(AngleDm.TryParse(input, out var value));
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [InlineData("+045 0", 45)]
    [InlineData("-090 0", -90)]
    [InlineData("060 0", 60)]
    [InlineData("180 0", 180)]
    [InlineData("089 0", 89)]
    [InlineData("-289 0", -289)]
    [InlineData("-054 0", -54)]
    [InlineData("+045", 45)]
    [InlineData("-090", -90)]
    [InlineData("060", 60)]
    [InlineData("180", 180)]
    [InlineData("089", 89)]
    [InlineData("-289", -289)]
    [InlineData("-054", -54)]
    [InlineData("001054000", 1054000)]
    public void CheckValidAngleDegValues(string input, double expectedValue)
    {
        // Проверка парсинга
        Assert.True(AngleDm.TryParse(input, out var value));
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [InlineData("00 30", 30.0 / 60.0, "00°30.00′", "en-US")]
    [InlineData("00 1", 1.0 / 60.0, "00°01.00′", "en-US")]
    [InlineData("00 09", 9.0 / 60.0, "00°09.00′", "en-US")]
    [InlineData("00 9", 9.0 / 60.0, "00°09.00′", "en-US")]
    [InlineData("00 59", 59.0 / 60.0, "00°59.00′", "en-US")]
    [InlineData("00 30.24", 30.24 / 60.0, "00°30.24′", "en-US")]
    [InlineData("-00 1.12345", -1.12345 / 60.0, "-00°01.12′", "en-US")]
    [InlineData("00 09.999", 9.999 / 60.0, "00°10.00′", "en-US")]
    [InlineData("-00 9.11", -9.11 / 60.0, "-00°09.11′", "en-US")]
    [InlineData("00 59.99", 59.99 / 60.0, "00°59.99′", "en-US")]
    [InlineData("00 59.999", 59.999 / 60.0, "01°00.00′", "en-US")]
    [InlineData("00 30", 30.0 / 60.0, "00°30,00′", "ru-RU")]
    [InlineData("00 1", 1.0 / 60.0, "00°01,00′", "ru-RU")]
    [InlineData("00 09", 9.0 / 60.0, "00°09,00′", "ru-RU")]
    [InlineData("00 9", 9.0 / 60.0, "00°09,00′", "ru-RU")]
    [InlineData("00 59", 59.0 / 60.0, "00°59,00′", "ru-RU")]
    [InlineData("00 30.24", 30.24 / 60.0, "00°30,24′", "ru-RU")]
    [InlineData("-00 1.12345", -1.12345 / 60.0, "-00°01,12′", "ru-RU")]
    [InlineData("00 09.999", 9.999 / 60.0, "00°10,00′", "ru-RU")]
    [InlineData("-00 9.11", -9.11 / 60.0, "-00°09,11′", "ru-RU")]
    [InlineData("00 59.99", 59.99 / 60.0, "00°59,99′", "ru-RU")]
    [InlineData("00 59.999", 59.999 / 60.0, "01°00,00′", "ru-RU")]
    public void CheckValidAngleMinuteValues(
        string input,
        double expectedValue,
        string expectedPrint,
        string culture
    )
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.CurrentCulture = new CultureInfo(culture);

        // Проверка парсинга
        Assert.True(AngleDm.TryParse(input, out var value));
        Assert.Equal(expectedValue, value);

        // Проверка печати
        Assert.Equal(expectedPrint, AngleDm.PrintDm(value));
    }
}
