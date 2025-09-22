using System.Globalization;
using System.Threading;
using Xunit;

namespace Asv.Common.Test.Other;

public class AngleMsTest
{
    [Theory]
    [InlineData("en-US", "2.40", 2.40)]
    [InlineData("en-US", "-3.40", -3.40)]
    [InlineData("en-US", "-0.410", -0.41)]
    [InlineData("en-US", "0.410", 0.41)]
    [InlineData("ru-RU", "2,40", 2.40)]
    [InlineData("ru-RU", "-3,40", -3.40)]
    [InlineData("ru-RU", "-0,410", -0.41)]
    [InlineData("ru-RU", "0,410", 0.41)]
    public void Check_double_values(string culture, string input, double expected)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.CurrentCulture = new CultureInfo(culture);
        Assert.True(AngleMs.TryParse(input, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("ru-RU", "-0 0")]
    [InlineData("ru-RU", "+0 0")]
    [InlineData("ru-RU", " -0 0")]
    [InlineData("ru-RU", " +0 0")]
    [InlineData("ru-RU", " 0 0")]
    [InlineData("ru-RU", "0 0 ")]
    [InlineData("en-US", "-0 0")]
    [InlineData("en-US", "+0 0")]
    [InlineData("en-US", " -0 0")]
    [InlineData("en-US", " +0 0")]
    [InlineData("en-US", " 0 0")]
    [InlineData("en-US", "0 0 ")]
    public void TryParse_Success_WithPlusMinusSymbol(string culture, string input)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.CurrentCulture = new CultureInfo(culture);
        Assert.True(AngleMs.TryParse(input, out var value));
        Assert.Equal(0, value);
    }

    [Theory]
    [InlineData("ru-RU", @"00' 00""")]
    [InlineData("ru-RU", @"00' 00 ")]
    [InlineData("ru-RU", @"00' 00"" ")]
    [InlineData("ru-RU", @"00′ 00""")]
    [InlineData("ru-RU", @"00′ 00 """)]
    [InlineData("ru-RU", @"00′ 00"" ")]
    [InlineData("ru-RU", @"00′ 00 ")]
    [InlineData("ru-RU", @"00' 00 """)]
    [InlineData("en-US", @"00' 00""")]
    [InlineData("en-US", @"00' 00 ")]
    [InlineData("en-US", @"00' 00"" ")]
    [InlineData("en-US", @"00′ 00""")]
    [InlineData("en-US", @"00′ 00 """)]
    [InlineData("en-US", @"00′ 00"" ")]
    [InlineData("en-US", @"00′ 00 ")]
    [InlineData("en-US", @"00' 00 """)]
    public void CheckMinuteSymbols(string culture, string input)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.CurrentCulture = new CultureInfo(culture);
        Assert.True(AngleMs.TryParse(input, out var value));
        Assert.Equal(0, value);
    }

    [Theory]
    [InlineData("ru-RU", "30 00", 30.0 / 60.0, "30′00,00˝")]
    [InlineData("ru-RU", "1 00", 1.0 / 60.0, "01′00,00˝")]
    [InlineData("ru-RU", "09 00", 9.0 / 60.0, "09′00,00˝")]
    [InlineData("ru-RU", "9 00", 9.0 / 60.0, "09′00,00˝")]
    [InlineData("ru-RU", "59 00", 59.0 / 60.0, "59′00,00˝")]
    [InlineData("ru-RU", "120 30", (120.0 / 60.0) + (30.0 / 3600.0), "120′30,00˝")]
    [InlineData("ru-RU", "-92 1", (-92.0 / 60.0) - (1.0 / 3600.0), "92′01,00˝")]
    [InlineData("ru-RU", "10000 09.14", (10000.0 / 60.0) + (9.14 / 3600.0), "10000′09,14˝")]
    [InlineData("en-US", "30 00", 30.0 / 60.0, "30′00.00˝")]
    [InlineData("en-US", "1 00", 1.0 / 60.0, "01′00.00˝")]
    [InlineData("en-US", "09 00", 9.0 / 60.0, "09′00.00˝")]
    [InlineData("en-US", "9 00", 9.0 / 60.0, "09′00.00˝")]
    [InlineData("en-US", "59 00", 59.0 / 60.0, "59′00.00˝")]
    [InlineData("en-US", "120 30", (120.0 / 60.0) + (30.0 / 3600.0), "120′30.00˝")]
    [InlineData("en-US", "-92 1", (-92.0 / 60.0) - (1.0 / 3600.0), "92′01.00˝")]
    [InlineData("en-US", "10000 09.14", (10000.0 / 60.0) + (9.14 / 3600.0), "10000′09.14˝")]
    public void CheckValidAngleMsMinuteValues(
        string culture,
        string input,
        double expectedValue,
        string expectedOutput
    )
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.CurrentCulture = new CultureInfo(culture);
        Assert.True(AngleMs.TryParse(input, out var value));
        Assert.Equal(expectedValue, value);
        Assert.Equal(expectedOutput, AngleMs.PrintMs(value));
    }

    [Theory]
    [InlineData("ru-RU", "00 01", 1.0 / 3600.0)]
    [InlineData("ru-RU", "-00 1", -1.0 / 3600.0)]
    [InlineData("ru-RU", "00 09", 9.0 / 3600.0)]
    [InlineData("ru-RU", "-00 9", -9.0 / 3600.0)]
    [InlineData("ru-RU", "00 59", 59.0 / 3600.0)]
    [InlineData("ru-RU", "-00 01.001", -1.001 / 3600.0)]
    [InlineData("ru-RU", "00 1.001", 1.001 / 3600.0)]
    [InlineData("ru-RU", "-00 09.001", -9.001 / 3600.0)]
    [InlineData("ru-RU", "00 9.001", 9.001 / 3600.0)]
    [InlineData("ru-RU", "-00 59.001", -59.001 / 3600.0)]
    [InlineData("en-US", "00 01", 1.0 / 3600.0)]
    [InlineData("en-US", "-00 1", -1.0 / 3600.0)]
    [InlineData("en-US", "00 09", 9.0 / 3600.0)]
    [InlineData("en-US", "-00 9", -9.0 / 3600.0)]
    [InlineData("en-US", "00 59", 59.0 / 3600.0)]
    [InlineData("en-US", "-00 01.001", -1.001 / 3600.0)]
    [InlineData("en-US", "00 1.001", 1.001 / 3600.0)]
    [InlineData("en-US", "-00 09.001", -9.001 / 3600.0)]
    [InlineData("en-US", "00 9.001", 9.001 / 3600.0)]
    [InlineData("en-US", "-00 59.001", -59.001 / 3600.0)]
    public void CheckValidAngleMsSecondValues(string culture, string input, double expectedValue)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.CurrentCulture = new CultureInfo(culture);
        Assert.True(AngleMs.TryParse(input, out var value));
        Assert.Equal(expectedValue, value);
    }
}
