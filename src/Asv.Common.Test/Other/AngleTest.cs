using Xunit;

namespace Asv.Common.Test;

public class AngleTest
{
    [Theory]
    [InlineData("2.40", 2.40, "en-US")]
    [InlineData("-3.40", -3.40, "en-US")]
    [InlineData("2,40", 2.40, "ru-RU")]
    [InlineData("-3,40", -3.40, "ru-RU")]
    public void Check_double_values(string input, double expectedValue, string culture)
    {
        var value = 0.0;
        var cultureInfo = new System.Globalization.CultureInfo(culture);
        Assert.True(Angle.TryParse(input, out value));
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [InlineData("-0 0 0", 0, "en-US")]
    [InlineData("+0 0 0", 0, "en-US")]
    [InlineData(" -0 0 0", 0, "en-US")]
    [InlineData(" +0 0 0", 0, "en-US")]
    [InlineData(" 0 0 0", 0, "en-US")]
    [InlineData("0 0 0 ", 0, "en-US")]
    [InlineData("-0 0 0", 0, "ru-RU")]
    [InlineData("+0 0 0", 0, "ru-RU")]
    [InlineData(" -0 0 0", 0, "ru-RU")]
    [InlineData(" +0 0 0", 0, "ru-RU")]
    [InlineData(" 0 0 0", 0, "ru-RU")]
    [InlineData("0 0 0 ", 0, "ru-RU")]
    public void CheckPlusAndMinus(string input, double expectedValue, string culture)
    {
        var value = 0.0;
        var cultureInfo = new System.Globalization.CultureInfo(culture);
        Assert.True(Angle.TryParse(input, out value));
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [InlineData(@"0° 0' 0""", 0, "en-US")]
    [InlineData(@"0˚ 0' 0""", 0, "en-US")]
    [InlineData(@"0º 0' 0""", 0, "en-US")]
    [InlineData(@"0^ 0' 0""", 0, "en-US")]
    [InlineData(@"0~ 0' 0""", 0, "en-US")]
    [InlineData(@"0* 0' 0""", 0, "en-US")]
    [InlineData(@"0 0' 0""", 0, "en-US")]
    [InlineData(@"0° 0' 0""", 0, "ru-RU")]
    [InlineData(@"0˚ 0' 0""", 0, "ru-RU")]
    [InlineData(@"0º 0' 0""", 0, "ru-RU")]
    [InlineData(@"0^ 0' 0""", 0, "ru-RU")]
    [InlineData(@"0~ 0' 0""", 0, "ru-RU")]
    [InlineData(@"0* 0' 0""", 0, "ru-RU")]
    [InlineData(@"0 0' 0""", 0, "ru-RU")]
    public void CheckDegreeSymbols(string input, double expectedValue, string culture)
    {
        var value = 0.0;
        var cultureInfo = new System.Globalization.CultureInfo(culture);
        Assert.True(Angle.TryParse(input, out value));
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [InlineData("00 30 00", 30.0 / 60.0, "en-US")]
    [InlineData("00 1 00", 1.0 / 60.0, "en-US")]
    [InlineData("00 09 00", 9.0 / 60.0, "en-US")]
    [InlineData("00 9 00", 9.0 / 60.0, "en-US")]
    [InlineData("00 59 00", 59.0 / 60.0, "en-US")]
    [InlineData("00 30 00", 30.0 / 60.0, "ru-RU")]
    [InlineData("00 1 00", 1.0 / 60.0, "ru-RU")]
    [InlineData("00 09 00", 9.0 / 60.0, "ru-RU")]
    [InlineData("00 9 00", 9.0 / 60.0, "ru-RU")]
    [InlineData("00 59 00", 59.0 / 60.0, "ru-RU")]
    public void CheckValidAngleMinuteValues(string input, double expectedValue, string culture)
    {
        var value = 0.0;
        var cultureInfo = new System.Globalization.CultureInfo(culture);
        Assert.True(Angle.TryParse(input, out value));
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [InlineData("00 00 01", 1.0 / 3600.0, "en-US")]
    [InlineData("-00 00 1", -1.0 / 3600.0, "en-US")]
    [InlineData("00 00 09", 9.0 / 3600.0, "en-US")]
    [InlineData("-00 00 9", -9.0 / 3600.0, "en-US")]
    [InlineData("00 00 59", 59.0 / 3600.0, "en-US")]
    [InlineData("-00 00 01.001", -1.001 / 3600.0, "en-US")]
    [InlineData("00 00 1.001", 1.001 / 3600.0, "en-US")]
    [InlineData("-00 00 09.001", -9.001 / 3600.0, "en-US")]
    [InlineData("00 00 9.001", 9.001 / 3600.0, "en-US")]
    [InlineData("-00 00 59.001", -59.001 / 3600.0, "en-US")]
    [InlineData("-00 00 59,001", -59.001 / 3600.0, "ru-RU")]
    [InlineData("00 00 59,001", 59.001 / 3600.0, "ru-RU")]
    public void CheckValidAngleSecondValues(string input, double expectedValue, string culture)
    {
        var value = 0.0;
        var cultureInfo = new System.Globalization.CultureInfo(culture);
        Assert.True(Angle.TryParse(input, out value));
        Assert.Equal(expectedValue, value);
    }
}
