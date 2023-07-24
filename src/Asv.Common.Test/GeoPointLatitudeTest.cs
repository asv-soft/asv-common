using Xunit;

namespace Asv.Common.Test;

public class GeoPointLatitudeTest
{
    [Fact]
    public void CheckPlusAndMinus()
    {
        var value = 0.0;
        Assert.True(GeoPointLatitude.TryParse("+0 0 0",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 0 0+",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 0 0 ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 0 0",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("-0 0 0",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("+0 0 0 ",out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckDegreeSymbols()
    {
        var value = 0.0;
        Assert.True(GeoPointLatitude.TryParse(@"0° 0' 0""",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"0˚ 0' 0""",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"0º 0' 0""",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"0^ 0' 0""",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"0~ 0' 0""",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"0* 0' 0""",out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckMinuteSymbols()
    {
        var value = 0.0;
        Assert.True(GeoPointLatitude.TryParse(@"000° 00' 00""",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00' 00 ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00' 00"" ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00′ 00""",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00′ 00 """,out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00′ 00"" ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00′ 00 ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00' 00 """,out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckSecondSymbols()
    {
        var value = 0.0;
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00\"",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00\" N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00.000\" N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00¨ N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00¨ ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00.000¨ N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00¨ n",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00˝ n",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00˝ ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00.000˝ n",out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckFullDmsWithDifferingSpaces()
    {
        var value = 0.0;
        Assert.True(GeoPointLatitude.TryParse("0°00'00˝N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 °00'00˝N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 ° 00'00˝N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 ° 00 '00˝N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 ° 00 ' 00˝N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 ° 00 ' 00 ˝N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 ° 00 ' 00 ˝ N",out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckFullDmsPrefixSuffix()
    {
        var value = 0.0;
        Assert.True(GeoPointLatitude.TryParse("0° 00' 00˝N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0° 00' 00˝",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("N0° 00' 00˝",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("00° 00'00˝",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("N0° 00' 00˝N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0° 00' 00˝ N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0° 00' 00˝S",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("S0° 00' 00˝",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("S0° 00' 00˝S",out value));
        Assert.Equal(0,value);
        
        Assert.False(GeoPointLatitude.TryParse("S0° 00' 00˝N",out value));
        Assert.False(GeoPointLatitude.TryParse("N0° 00' 00˝S",out value));
        Assert.False(GeoPointLatitude.TryParse("+0° 00' 00˝S",out value));
        Assert.False(GeoPointLatitude.TryParse("-0° 00' 00˝N",out value));
    }

    [Fact]
    public void CheckAllZeros()
    {
        var value = 0.0;
        Assert.True(GeoPointLatitude.TryParse("00 00 00 N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("00 00 0 N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("00 0 00 N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("00 0 0 N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 00 00 N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 0 00 N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 0 0 N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 00 0 N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 0 00.000 N",out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckIncompleteEntries()
    {
        var value = 0.0;
        Assert.True(GeoPointLatitude.TryParse("0 0 0 N",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 0 N",out value));
        Assert.Equal(0,value);
        Assert.False(GeoPointLatitude.TryParse("0 N",out value));
    }

    [Fact]
    public void CheckValidLatitudeDegValues()
    {
        var value = 0.0;
        Assert.True(GeoPointLatitude.TryParse("01 00 00 N",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLatitude.TryParse("1 00 00 N",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLatitude.TryParse("09 00 00 N",out value));
        Assert.Equal(9,value);
        Assert.True(GeoPointLatitude.TryParse("9 00 00 N",out value));
        Assert.Equal(9,value);
        Assert.True(GeoPointLatitude.TryParse("89 00 00 N",out value));
        Assert.Equal(89,value);
        
        Assert.False(GeoPointLatitude.TryParse("90 00 01 N",out value));
        
        Assert.Equal(90 + 1.0/3600.0,value);
        Assert.True(GeoPointLatitude.TryParse("+00 00 00 N ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("+0 00 00 N ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("+01 00 00 N",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLatitude.TryParse("+1 00 00 N",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLatitude.TryParse("+09 00 00 N",out value));
        Assert.Equal(9,value);
        Assert.True(GeoPointLatitude.TryParse("+9 00 00 N",out value));
        Assert.Equal(9,value);
        Assert.True(GeoPointLatitude.TryParse("+89 00 00 N",out value));
        Assert.Equal(89,value);
        
        Assert.False(GeoPointLatitude.TryParse("+90 00 01 N",out value));
        
        Assert.False(GeoPointLatitude.TryParse("-00 00 00 N",out value));
        Assert.False(GeoPointLatitude.TryParse("-0 00 00 N",out value));
        Assert.False(GeoPointLatitude.TryParse("-01 00 00 N",out value));
        Assert.False(GeoPointLatitude.TryParse("-1 00 00 N",out value));
        
        Assert.False(GeoPointLatitude.TryParse("-09 00 00 N",out value));
        Assert.False(GeoPointLatitude.TryParse("-9 00 00 N",out value));
        Assert.False(GeoPointLatitude.TryParse("-89 00 00 N",out value));
        Assert.False(GeoPointLatitude.TryParse("-90 00 01 N",out value));
        
        Assert.True(GeoPointLatitude.TryParse("00 00 00 ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("+0 00 00",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("+01 00 00",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLatitude.TryParse("+1 00 00",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLatitude.TryParse("+09 00 00",out value));
        Assert.Equal(9,value);
        Assert.True(GeoPointLatitude.TryParse("+9 00 00",out value));
        Assert.Equal(9,value);
        Assert.True(GeoPointLatitude.TryParse("+89 00 00",out value));
        Assert.Equal(89,value);
        
        Assert.False(GeoPointLatitude.TryParse("+90 00 01",out value));
        
    }

    [Fact]
    public void CheckValidLatitudeMinuteValues()
    {
        var value = 0.0;
        Assert.True(GeoPointLatitude.TryParse("00 01 00 N",out value));
        Assert.Equal(1.0/60.0,value);
        Assert.True(GeoPointLatitude.TryParse("00 1 00 N",out value));
        Assert.Equal(1.0/60.0,value);
        Assert.True(GeoPointLatitude.TryParse("00 09 00 N",out value));
        Assert.Equal(9.0/60.0,value);
        Assert.True(GeoPointLatitude.TryParse("00 9 00 N",out value));
        Assert.Equal(9.0/60.0,value);
        Assert.True(GeoPointLatitude.TryParse("00 59 00 N",out value));
        Assert.Equal(59.0/60.0,value);
    }

    [Fact]
    public void CheckValidLatitudeSecondValues()
    {
        var value = 0.0;
        Assert.True(GeoPointLatitude.TryParse("00 00 01 N",out value));
        Assert.Equal(1.0/3600.0,value);
        Assert.True(GeoPointLatitude.TryParse("00 00 1 N",out value));
        Assert.Equal(1.0/3600.0,value);
        Assert.True(GeoPointLatitude.TryParse("00 00 09 N",out value));
        Assert.Equal(9.0/3600.0,value);
        Assert.True(GeoPointLatitude.TryParse("00 00 9 N",out value));
        Assert.Equal(9.0/3600.0,value);
        Assert.True(GeoPointLatitude.TryParse("00 00 59 N",out value));
        Assert.Equal(59.0/3600.0,value);
        Assert.True(GeoPointLatitude.TryParse("00 00 01.001 N",out value));
        Assert.Equal(1.0/3600.0,value);
        Assert.True(GeoPointLatitude.TryParse("00 00 1.001 N",out value));
        Assert.Equal(1.0/3600.0,value);
        Assert.True(GeoPointLatitude.TryParse("00 00 09.001 N",out value));
        Assert.Equal(9.0/3600.0,value);
        Assert.True(GeoPointLatitude.TryParse("00 00 9.001 N",out value));
        Assert.Equal(9.0/3600.0,value);
        Assert.True(GeoPointLatitude.TryParse("00 00 59.001 N",out value));
        Assert.Equal(59.0/3600.0,value);
    }
}