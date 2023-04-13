using Xunit;

namespace Asv.Common.Test;

public class GeoPointLongitudeTest
{
    [Fact]
    public void CheckPlusAndMinus()
    {
        var value = 0.0;
        Assert.True(GeoPointLongitude.TryParse("+0 0 0",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0 0 0+",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0 0 0 ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0 0 0",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("-0 0 0",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("+0 0 0 ",out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckDegreeSymbols()
    {
        var value = 0.0;
        Assert.True(GeoPointLongitude.TryParse("0° 0' 0\"",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0˚ 0' 0\"",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0º 0' 0\"",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0^ 0' 0\"",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0~ 0' 0\"",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0* 0' 0\"",out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckMinuteSymbols()
    {
        var value = 0.0;
        Assert.True(GeoPointLongitude.TryParse(@"000° 00' 00""",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse(@"000° 00' 00 ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse(@"000° 00' 00"" ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse(@"000° 00′ 00""",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse(@"000° 00′ 00 """,out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse(@"000° 00′ 00"" ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse(@"000° 00′ 00 ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse(@"000° 00' 00 """,out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckSecondSymbols()
    {
        var value = 0.0;
        Assert.True(GeoPointLongitude.TryParse("000° 00' 00\"",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000° 00' 00\" E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000° 00' 00.000\" E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000° 00' 00¨ E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000° 00' 00¨",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000° 00' 00.000¨ E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000° 00' 00˝ E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000° 00' 00˝ 0°",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000° 00' 00.000˝ E",out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckFullDmsWithDifferingSpaces()
    {
        var value = 0.0;
        Assert.True(GeoPointLongitude.TryParse("000°00'00˝E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 °00'00˝E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 ° 00'00˝E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000  °00 '00˝E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 ° 00 ' 00˝E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 ° 00 '00 ˝ E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 ° 00 ' 00 ˝ E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 ° 00 ' 00 ˝ E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 ° 00 ' 00 ˝ E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 ° 00 ' 00 ˝ E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 ° 00 ' 00 ˝ E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 ° 00 ' 00 ˝ E",out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckFullDmsPrefixSuffix()
    {
        var value = 0.0;
        Assert.True(GeoPointLongitude.TryParse("000° 00'00˝E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000° 00'00˝",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("E000° 00'00˝",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0° 00' 00˝ E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("E000° 00'00˝E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000° 00'00˝W",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("W000° 00'00˝",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("W000° 00'00˝W",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("E000° 00'00˝W",out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckAllZeros()
    {
        var value = 0.0;
        Assert.True(GeoPointLongitude.TryParse("000 00 00 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("00 00 00 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 00 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 0 00 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("00 00 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("00 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0 00 00 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0 0 00 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0 0 0.000 E",out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckIncompleteEntries()
    {
        var value = 0.0;
        Assert.True(GeoPointLongitude.TryParse("000 00 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0 0 00 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0 0 0E",out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckValidLongitudeDegValues()
    {
        var value = 0.0;
        Assert.True(GeoPointLongitude.TryParse("001 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("01 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("1 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("10 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("99 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("100 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("179 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("79 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("180 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("001° 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("01° 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("1° 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("10° 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("99° 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("100° 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("179° 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("79° 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("180° 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("001˚ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("01˚ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("1˚ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("10˚ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("99˚ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("100˚ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("179˚ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("79˚ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("180˚ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("001º 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("01º 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("1º 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("10º 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("99º 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("100º 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("179º 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("79º 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("180º 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("001^ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("01^ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("1^ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("10^ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("99^ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("100^ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("179^ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("79^ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("180^ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("001~ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("01~ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("1~ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("10~ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("99~ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("100~ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("179~ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("79~ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("180~ 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("001* 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("01* 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("1* 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("10* 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("99* 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("100* 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("179* 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("79* 0 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("180* 0 0 E",out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckValidLongitudeMinuteValues()
    {
        var value = 0.0;
        Assert.True(GeoPointLongitude.TryParse("000 01 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 1 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 9 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 09 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 10 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 59 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 5 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 01' 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 1' 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 9' 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 09' 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 10' 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 59' 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 5' 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 01′ 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 1′ 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 9′ 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 09′ 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 10′ 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 59′ 0 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000 5′ 0 E",out value));
        Assert.Equal(0,value);
    }
}