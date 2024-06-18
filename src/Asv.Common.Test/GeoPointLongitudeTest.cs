using Xunit;

namespace Asv.Common.Test;

public class GeoPointLongitudeTest
{
    [Theory]
    [InlineData("+0 0 0", 0)]
    [InlineData("0 0 0+", 0)]
    [InlineData("0 0 0 ", 0)]
    [InlineData("0 0 0", 0)]
    [InlineData("-0 0 0", 0)]
    [InlineData("+0 0 0 ", 0)]
    public void CheckPlusAndMinus(string input, double expected)
    {
        Assert.True(GeoPointLongitude.TryParse(input, out double value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("0° 0' 0\"", 0)]
    [InlineData("0˚ 0' 0\"", 0)]
    [InlineData("0º 0' 0\"", 0)]
    [InlineData("0^ 0' 0\"", 0)]
    [InlineData("0~ 0' 0\"", 0)]
    [InlineData("0* 0' 0\"", 0)]
    [InlineData("0°0'0\"", 0)]
    [InlineData("0˚0'0\"", 0)]
    [InlineData("0º0'0\"", 0)]
    [InlineData("0^0'0\"", 0)]
    [InlineData("0~0'0\"", 0)]
    [InlineData("0*0'0\"", 0)]
    [InlineData("00'0\"", null)]
    public void CheckDegreeSymbols(string input, double? expected)
    {
        bool result = GeoPointLongitude.TryParse(input, out double value);
        if (expected.HasValue)
        {
            Assert.True(result);
            Assert.Equal(expected.Value, value);
        }
        else
        {
            Assert.False(result);
        }
    }

    [Theory]
    [InlineData(@"000° 00' 00""", 0)]
    [InlineData(@"000° 00' 00 ", 0)]
    [InlineData(@"000° 00' 00"" ", 0)]
    [InlineData(@"000° 00′ 00""", 0)]
    [InlineData(@"000° 00′ 00 """, 0)]
    [InlineData(@"000° 00′ 00"" ", 0)]
    [InlineData(@"000° 00′ 00 ", 0)]
    [InlineData(@"000° 00' 00 """, 0)]
    [InlineData(@"000° 0000 """, 0)]
    public void CheckMinuteSymbols(string input, double expected)
    {
        Assert.True(GeoPointLongitude.TryParse(input, out double value));
        Assert.Equal(expected, value);
    }
    [Theory]
    [InlineData("000° 00' 00\"", 0)]
    [InlineData("000° 00' 00\" E", 0)]
    [InlineData("000° 00' 00.000\" E", 0)]
    [InlineData("000° 00' 00¨ E", 0)]
    [InlineData("000° 00' 00¨", 0)]
    [InlineData("000° 00' 00.000¨ E", 0)]
    [InlineData("000° 00' 00˝ E", 0)]
    [InlineData("000° 00' 00.000˝ E", 0)]
    [InlineData("000° 00' 00", 0)]
    [InlineData("000° 00' 00 E", 0)]
    [InlineData("000° 00' 00˝ 0°", null)]
    public void CheckSecondSymbols(string input, double? expected)
    {
        bool result = GeoPointLongitude.TryParse(input, out double value);
        if (expected.HasValue)
        {
            Assert.True(result);
            Assert.Equal(expected.Value, value);
        }
        else
        {
            Assert.False(result);
        }
    }

    [Theory]
    [InlineData("000°00'00˝E", 0)]
    [InlineData("000 °00'00˝E", 0)]
    [InlineData("000 ° 00'00˝E", 0)]
    [InlineData("000  °00 '00˝E", 0)]
    [InlineData("000 ° 00 ' 00˝E", 0)]
    [InlineData("000 ° 00 '00 ˝ E", 0)]
    [InlineData("000 ° 00 ' 00 ˝ E", 0)]
    public void CheckFullDmsWithDifferingSpaces(string input, double expected)
    {
        Assert.True(GeoPointLongitude.TryParse(input, out double value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("000° 00'00˝E", 0)]
    [InlineData("000° 00'00˝", 0)]
    [InlineData("E000° 00'00˝", 0)]
    [InlineData("0° 00' 00˝ E", 0)]
    [InlineData("E000° 00'00˝E", 0)]
    [InlineData("000° 00'00˝W", 0)]
    [InlineData("W000° 00'00˝", 0)]
    [InlineData("W000° 00'00˝W", 0)]
    [InlineData("E000° 00'00˝W", null)]
    public void CheckFullDmsPrefixSuffix(string input, double? expected)
    {
        bool result = GeoPointLongitude.TryParse(input, out double value);
        if (expected.HasValue)
        {
            Assert.True(result);
            Assert.Equal(expected.Value, value);
        }
        else
        {
            Assert.False(result);
        }
    }

    [Theory]
    [InlineData("85.536123", 85.536123)]
    [InlineData("-65,536", -65.536)]
    [InlineData("180,536", null)]
    public void Check_double_values(string input, double? expected)
    {
        bool result = GeoPointLongitude.TryParse(input, out double value);
        if (expected.HasValue)
        {
            Assert.True(result);
            Assert.Equal(expected.Value, value);
        }
        else
        {
            Assert.False(result);
        }
    }

    [Theory]
    [InlineData("000 00 00 E", 0)]
    [InlineData("00 00 00 E", 0)]
    [InlineData("000 00 0 E", 0)]
    [InlineData("000 0 00 E", 0)]
    [InlineData("000 0 0 E", 0)]
    [InlineData("00 00 0 E", 0)]
    [InlineData("00 0 0 E", 0)]
    [InlineData("0 00 00 E", 0)]
    [InlineData("0 0 00 E", 0)]
    [InlineData("0 0 0 E", 0)]
    [InlineData("0 0 0.000 E", 0)]
    public void CheckAllZeros(string input, double expected)
    {
        Assert.True(GeoPointLongitude.TryParse(input, out double value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("000 00 E", 0)]
    [InlineData("0 0 00 E", 0)]
    [InlineData("0 0 0E", 0)]
    [InlineData("000 E", 0)]
    public void CheckIncompleteEntries(string input, double? expected)
    {
        bool result = GeoPointLongitude.TryParse(input, out double value);
        if (expected.HasValue)
        {
            Assert.True(result);
            Assert.Equal(expected.Value, value);
        }
        else
        {
            Assert.False(result);
            Assert.Equal(double.NaN, value);
        }
    }

    [Theory]
    [InlineData("2 40", 2 + 40d/60d)]
    [InlineData("15 59 45", 15 + 59d/60d + 45d/3600d)]
    [InlineData("0 1 0 W", -1d/60d)]
    public void CheckDmsWithShortValues(string input, double expected)
    {
        Assert.True(GeoPointLongitude.TryParse(input, out double value));
        Assert.Equal(expected, value, 6); // using precision of 6 decimal places
    }
    [Theory]
    [InlineData("001 00 00.00 E", 1)]
    [InlineData("01 00 00.00 E", 1)]
    [InlineData("1 00 00.00 E", 1)]
    [InlineData("10 00 00.00 E", 10)]
    [InlineData("99 0 0 E", 99)]
    [InlineData("100 0 0 E", 100)]
    [InlineData("179 0 0 E", 179)]
    [InlineData("79 0 0 E", 79)]
    [InlineData("180 0 0 E", 180)]
    [InlineData("001° 0 0 E", 1)]
    [InlineData("01° 0 0 E", 1)]
    [InlineData("1° 0 0 E", 1)]
    [InlineData("10° 0 0 E", 10)]
    [InlineData("99° 0 0 E", 99)]
    [InlineData("100° 0 0 E", 100)]
    [InlineData("179° 0 0 E", 179)]
    [InlineData("79° 0 0 E", 79)]
    [InlineData("180° 0 0 E", 180)]
    [InlineData("001˚ 0 0 E", 1)]
    [InlineData("01˚ 0 0 E", 1)]
    [InlineData("1˚ 0 0 E", 1)]
    [InlineData("10˚ 0 0 E", 10)]
    [InlineData("99˚ 0 0 E", 99)]
    [InlineData("100˚ 0 0 E", 100)]
    [InlineData("179˚ 0 0 E", 179)]
    [InlineData("79˚ 0 0 E", 79)]
    [InlineData("180˚ 0 0 E", 180)]
    [InlineData("001º 0 0 E", 1)]
    [InlineData("01º 0 0 E", 1)]
    [InlineData("1º 0 0 E", 1)]
    [InlineData("10º 0 0 E", 10)]
    [InlineData("99º 0 0 E", 99)]
    [InlineData("100º 0 0 E", 100)]
    [InlineData("179º 0 0 E", 179)]
    [InlineData("79º 0 0 E", 79)]
    [InlineData("180º 0 0 E", 180)]
    [InlineData("001^ 0 0 E", 1)]
    [InlineData("01^ 0 0 E", 1)]
    [InlineData("1^ 0 0 E", 1)]
    [InlineData("10^ 0 0 E", 10)]
    [InlineData("99^ 0 0 E", 99)]
    [InlineData("100^ 0 0 E", 100)]
    [InlineData("179^ 0 0 E", 179)]
    [InlineData("79^ 0 0 E", 79)]
    [InlineData("180^ 0 0 E", 180)]
    [InlineData("001~ 0 0 E", 1)]
    [InlineData("01~ 0 0 E", 1)]
    [InlineData("1~ 0 0 E", 1)]
    [InlineData("10~ 0 0 E", 10)]
    [InlineData("99~ 0 0 E", 99)]
    [InlineData("100~ 0 0 E", 100)]
    [InlineData("179~ 0 0 E", 179)]
    [InlineData("79~ 0 0 E", 79)]
    [InlineData("180~ 0 0 E", 180)]
    [InlineData("001* 0 0 E", 1)]
    [InlineData("01* 0 0 E", 1)]
    [InlineData("1* 0 0 E", 1)]
    [InlineData("10* 0 0 E", 10)]
    [InlineData("99* 0 0 E", 99)]
    [InlineData("100* 0 0 E", 100)]
    [InlineData("179* 0 0 E", 179)]
    [InlineData("79* 0 0 E", 79)]
    [InlineData("180* 0 0 E", 180)]
    public void CheckValidLongitudeDegValues(string input, double expected)
    {
        Assert.True(GeoPointLongitude.TryParse(input, out double value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("000 01 0 E", 1.0/60.0)]
    [InlineData("000 1 0 E", 1.0/60.0)]
    [InlineData("000 9 0 E", 9.0/60.0)]
    [InlineData("000 09 0 E", 9.0/60.0)]
    [InlineData("000 10 0 E", 10.0/60.0)]
    [InlineData("000 59 0 E", 59.0/60.0)]
    [InlineData("000 5 0 E", 5.0/60.0)]
    [InlineData("000 01' 0 E", 1.0/60.0)]
    [InlineData("000 1' 0 E", 1.0/60.0)]
    [InlineData("000 9' 0 E", 9.0/60.0)]
    [InlineData("000 09' 0 E", 9.0/60.0)]
    [InlineData("000 10' 0 E", 10.0/60.0)]
    [InlineData("000 59' 0 E", 59.0/60.0)]
    [InlineData("000 5' 0 E", 5.0/60.0)]
    [InlineData("000 01′ 0 E", 1.0/60.0)]
    [InlineData("000 1′ 0 E", 1.0/60.0)]
    [InlineData("000 9′ 0 E", 9.0/60.0)]
    [InlineData("000 09′ 0 E", 9.0/60.0)]
    [InlineData("000 10′ 0 E", 10.0/60.0)]
    [InlineData("000 59′ 0 E", 59.0/60.0)]
    [InlineData("000 5′ 0 E", 5.0/60.0)]
    public void CheckValidLongitudeMinuteValues(string input, double expected)
    {
        Assert.True(GeoPointLongitude.TryParse(input, out double value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("000 00 01 E", 1.0/3600.0)]
    [InlineData("000 00 1 E", 1.0/3600.0)]
    [InlineData("000 00 09 E", 9.0/3600.0)]
    [InlineData("000 00 9 E", 9.0/3600.0)]
    [InlineData("000 00 59 E", 59.0/3600.0)]
    [InlineData("000 00 01.001 E", 1.001/3600.0)]
    [InlineData("000 00 1.001 E", 1.001/3600.0)]
    [InlineData("000 00 09.001 E", 9.001/3600.0)]
    [InlineData("000 00 9.001 E", 9.001/3600.0)]
    [InlineData("000 00 59.001 E", 59.001/3600.0)]
    [InlineData("0000001E", 1.0/3600.0)]
    [InlineData("000001E", 1.0/3600.0)]
    [InlineData("0000009E", 9.0/3600.0)]
    [InlineData("000009E", 9.0/3600.0)]
    [InlineData("0000059E", 59.0/3600.0)]
    [InlineData("0000001.001E", 1.001/3600.0)]
    public void CheckValidLongitudeSecondValues(string input, double expected)
    {
        Assert.True(GeoPointLongitude.TryParse(input, out double value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("0612844.42E", 61.47900555555556)]
    public void Check_longitude_from_aip(string input, double expected)
    {
        Assert.True(GeoPointLongitude.TryParse(input, out double value));
        Assert.Equal(expected, value);
    }
    
    [Theory]
    [InlineData("068:25.0365W", -68.416667)]
    [InlineData("E068:25.0365", 68.416667)]
    [InlineData("68:25.0365W", -68.416667)]
    [InlineData("68:2.365W", -68.033333)]
    [InlineData("90:0.0W", -90.0)]
    [InlineData("90:00.000e", 90.0)]
    public void CheckValidLongitudeDegreesMinutes(string input, double expectedLatitude)
    {
        Assert.True(GeoPointLongitude.TryParse(input, out var value));
        Assert.Equal(expectedLatitude, value, 6);
    }
    
    [Theory]
    [InlineData("068:25. 0365W")]
    [InlineData("068:25 .0365W")]
    [InlineData("068:25 . 0365W")]
    public void CheckValidLongitudeDegreesMinutesWhiteSpaces(string input)
    {
        Assert.False(GeoPointLongitude.TryParse(input, out _));
    }
}