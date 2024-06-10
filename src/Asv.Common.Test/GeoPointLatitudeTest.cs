using Xunit;

namespace Asv.Common.Test;

public class GeoPointLatitudeTest
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
        Assert.True(GeoPointLatitude.TryParse(input, out double value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("65.536", 65.536)]
    [InlineData("-65,536", -65.536)]
    [InlineData("90,536", null)]
    public void Check_double_values(string input, double? expected)
    {
        bool result = GeoPointLatitude.TryParse(input, out double value);
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
    [InlineData(@"0° 0' 0""", 0)]
    [InlineData(@"0˚ 0' 0""", 0)]
    [InlineData(@"0º 0' 0""", 0)]
    [InlineData(@"0^ 0' 0""", 0)]
    [InlineData(@"0~ 0' 0""", 0)]
    [InlineData(@"0* 0' 0""", 0)]
    [InlineData(@"0 0' 0""", 0)]
    public void CheckDegreeSymbols(string input, double expected)
    {
        Assert.True(GeoPointLatitude.TryParse(input, out double value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData(@"00° 00' 0""", 0)]
    [InlineData(@"00° 00' 00 """, 0)]
    [InlineData(@"00° 00′ 00""", 0)]
    [InlineData(@"00° 00′ 00 """, 0)]
    public void CheckMinuteSymbols(string input, double expected)
    {
        Assert.True(GeoPointLatitude.TryParse(input, out double value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("00° 00' 00\"", 0)]
    [InlineData("00° 00' 00\" N", 0)]
    [InlineData("00° 00' 00.000\" N", 0)]
    [InlineData("00° 00' 00¨ N", 0)]
    [InlineData("00° 00' 00.000¨ N", 0)]
    [InlineData("00° 00' 00˝ n", 0)]
    public void CheckSecondSymbols(string input, double expected)
    {
        Assert.True(GeoPointLatitude.TryParse(input, out double value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("0°00'00˝N", 0)]
    [InlineData("0 °00'00˝N", 0)]
    [InlineData("0 ° 00 ' 00˝N", 0)]
    [InlineData("0 ° 00 ' 00 ˝ N", 0)]
    public void CheckFullDmsWithDifferingSpaces(string input, double expected)
    {
        Assert.True(GeoPointLatitude.TryParse(input, out double value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("2 40", 2 + 40d/60d)]
    [InlineData("15 59 45", 15 + 59d/60d + 45d/3600d)]
    [InlineData("0 1 0 S", -1d/60d)]
    [InlineData("15 59 45,15 S", -15 + -59d/60d + -45.15d/3600d)]
    public void CheckDmsWithShortValues(string input, double expected)
    {
        Assert.True(GeoPointLatitude.TryParse(input, out double value));
        Assert.Equal(expected, value, 6); // using precision of 6 decimal places
    }
    
    [Theory]
    [InlineData("00 00 00 N", 0)]
    [InlineData("00 00 0 N", 0)]
    [InlineData("00 0 00 N", 0)]
    [InlineData("00 0 0 N", 0)]
    [InlineData("0 00 00 N", 0)]
    [InlineData("0 0 00 N", 0)]
    [InlineData("0 0 0 N", 0)]
    [InlineData("0 00 0 N", 0)]
    [InlineData("0 0 00.000 N", 0)]
    public void CheckAllZeros(string input, double expected)
    {
        Assert.True(GeoPointLatitude.TryParse(input, out double value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("0 0 0 N", 0)]
    [InlineData("0 0 N", 0)]
    [InlineData("0 N", null)] // Assuming this should fail and not return a value.
    public void CheckIncompleteEntries(string input, double? expected)
    {
        bool result = GeoPointLatitude.TryParse(input, out double value);
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
    [InlineData("01 00 00 N", 1)]
    [InlineData("1 00 00 N", 1)]
    [InlineData("09 00 00 N", 9)]
    [InlineData("9 00 00 N", 9)]
    [InlineData("89 00 00 N", 89)]
    [InlineData("890000N", 89)]
    [InlineData("90 00 01 N", null)] // Assuming this should fail and not return a value.
    [InlineData("900001N", null)] // Assuming this should fail and not return a value.
    [InlineData("+00 00 00 N", 0)]
    [InlineData("+0 00 00 N", 0)]
    [InlineData("+01 00 00 N", 1)]
    [InlineData("+1 00 00 N", 1)]
    [InlineData("+09 00 00 N", 9)]
    [InlineData("+9 00 00 N", 9)]
    [InlineData("+89 00 00 N", 89)]
    [InlineData("+00000N", 0)]
    [InlineData("+010000N", 1)]
    [InlineData("+10000N", 10)] 
    [InlineData("+090000N", 9)]
    [InlineData("+90000N", 90)]
    [InlineData("+890000N", 89)]
    [InlineData("+90 00 01 N", null)] // Assuming this should fail and not return a value.
    [InlineData("-00 00 00 N", null)] // Assuming this should fail and not return a value.
    [InlineData("-0 00 00 N", null)] // Assuming this should fail and not return a value.
    [InlineData("-01 00 00 N", null)] // Assuming this should fail and not return a value.
    [InlineData("-1 00 00 N", null)] // Assuming this should fail and not return a value.
    [InlineData("-09 00 00 N", null)] // Assuming this should fail and not return a value.
    [InlineData("-9 00 00 N", null)] // Assuming this should fail and not return a value.
    [InlineData("-89 00 00 N", null)] // Assuming this should fail and not return a value.
    [InlineData("-90 00 01 N", null)] // Assuming this should fail and not return a value.
    [InlineData("+900001N", null)] // Assuming this should fail and not return a value.
    [InlineData("-000000 N", null)] // Assuming this should fail and not return a value.
    [InlineData("-00000N", null)] // Assuming this should fail and not return a value.
    [InlineData("-010000N", null)] // Assuming this should fail and not return a value.
    [InlineData("-10000N", null)] // Assuming this should fail and not return a value.
    [InlineData("-090000N", null)] // Assuming this should fail and not return a value.
    [InlineData("-90000N", null)] // Assuming this should fail and not return a value.
    [InlineData("-890000N", null)] // Assuming this should fail and not return a value.
    [InlineData("-900001N", null)] // Assuming this should fail and not return a value.
    [InlineData("00 00 00", 0)]
    [InlineData("+0 00 00", 0)]
    [InlineData("+01 00 00", 1)]
    [InlineData("+1 00 00", 1)]
    [InlineData("+09 00 00", 9)]
    [InlineData("+9 00 00", 9)]
    [InlineData("+89 00 00", 89)]
    [InlineData("+90 00 01", null)] // Assuming this should fail and not return a value.
    [InlineData("000000", 0)]
    [InlineData("+00000", 0)]
    [InlineData("+010000", 1)]
    [InlineData("+10000", 10)]
    [InlineData("+090000", 9)]
    [InlineData("+90000", 90)]
    [InlineData("+890000", 89)]
    [InlineData("+900001", null)] // Assuming this should fail and not return a value.
    public void CheckValidLatitudeDegValues(string input, double? expected)
    {
        bool result = GeoPointLatitude.TryParse(input, out double value);
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
    [InlineData("00 01 00 N", 1.0/60.0)]
    [InlineData("00 1 00 N", 1.0/60.0)]
    [InlineData("00 09 00 N", 9.0/60.0)]
    [InlineData("00 9 00 N", 9.0/60.0)]
    [InlineData("00 59 00 N", 59.0/60.0)]
    public void CheckValidLatitudeMinuteValues(string input, double expected)
    {
        Assert.True(GeoPointLatitude.TryParse(input, out double value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("00 00 01 N", 1.0/3600.0)]
    [InlineData("00 00 1 N", 1.0/3600.0)]
    [InlineData("00 00 09 N", 9.0/3600.0)]
    [InlineData("00 00 9 N", 9.0/3600.0)]
    [InlineData("00 00 59 N", 59.0/3600.0)]
    [InlineData("00 00 01.001 N", 1.001/3600.0)]
    [InlineData("00 00 1.001 N", 1.001/3600.0)]
    [InlineData("00 00 09.001 N", 9.001/3600.0)]
    [InlineData("00 00 9.001 N", 9.001/3600.0)]
    [InlineData("00 00 59.001 N", 59.001/3600.0)]
    [InlineData("000059N", 59.0/3600.0)]
    [InlineData("000001.001N", 1.001/3600.0)]
    [InlineData("00001.001N", 1.001/3600.0)]
    [InlineData("000009.001N", 9.001/3600.0)]
    [InlineData("00009.001N", 9.001/3600.0)]
    [InlineData("000059.001N", 59.001/3600.0)]
    public void CheckValidLatitudeSecondValues(string input, double expected)
    {
        Assert.True(GeoPointLatitude.TryParse(input, out double value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("45:54.454S", -45.907567)]
    [InlineData("45:54.454 S", -45.907567)]
    [InlineData("45:54.454N", 45.907567)]
    [InlineData("N 45:54.454", 45.907567)]
    [InlineData("90:00.000S", -90.0)]
    [InlineData("90:00.000N", 90.0)]
    [InlineData("00 :00.000N", 0.0)]
    [InlineData("00: 00.000S", 0.0)]
    public void CheckValidLatitudeDegreesMinutes(string input, double expectedLatitude)
    {
        Assert.True(GeoPointLatitude.TryParse(input, out var latitude));
        Assert.Equal(expectedLatitude, latitude, 6);
    }
    
    [Theory]
    [InlineData("S89:59 .999")]
    [InlineData("N45 : 00 . 000")]
    [InlineData("45:00. 000N")]
    [InlineData("89: 59. 999S")]
    [InlineData("45:00 .000S")]
    public void CheckValidLatitudeDegreesMinutesWhiteSpaces(string input)
    {
        Assert.False(GeoPointLatitude.TryParse(input, out _));
    }
}