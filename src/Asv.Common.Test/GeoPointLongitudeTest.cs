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
        Assert.True(GeoPointLongitude.TryParse("0°0'0\"",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0˚0'0\"",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0º0'0\"",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0^0'0\"",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0~0'0\"",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0*0'0\"",out value));
        Assert.Equal(0,value);
        Assert.False(GeoPointLongitude.TryParse("00'0\"",out value));
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
        Assert.True(GeoPointLongitude.TryParse(@"000° 0000 """,out value));
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
        Assert.False(GeoPointLongitude.TryParse("000° 00' 00˝ 0°",out value));
        Assert.True(GeoPointLongitude.TryParse("000° 00' 00.000˝ E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000° 00' 00",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("000° 00' 00 E",out value));
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
        Assert.False(GeoPointLongitude.TryParse("E000° 00'00˝W",out value));
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
        Assert.False(GeoPointLongitude.TryParse("000 E",out value));
        Assert.Equal(double.NaN,value);
        Assert.True(GeoPointLongitude.TryParse("0 0 00 E",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLongitude.TryParse("0 0 0E",out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckDmsWithShortValues()
    {
        var value = 0.0;
        Assert.True(GeoPointLongitude.TryParse("2 40",out value));
        Assert.Equal(2 + 40d/60d,value);
        Assert.Equal("2°40′0.00˝ E", GeoPointLongitude.PrintDms(value).Replace(",", "."));
        Assert.True(GeoPointLongitude.TryParse("15 59 45",out value));
        Assert.Equal(15 + 59d/60d + 45d/3600d,value);
        Assert.Equal("15°59′45.00˝ E", GeoPointLongitude.PrintDms(value).Replace(",", "."));
        Assert.True(GeoPointLongitude.TryParse("0 1 0 W",out value));
        Assert.Equal(-1d/60d,value);
        Assert.Equal("0°1′0.00˝ W", GeoPointLongitude.PrintDms(value).Replace(",", "."));
    }

    [Fact]
    public void CheckValidLongitudeDegValues()
    {
        var value = 0.0;
        Assert.True(GeoPointLongitude.TryParse("001 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("01 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("1 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("10 0 0 E",out value));
        Assert.Equal(10,value);
        Assert.True(GeoPointLongitude.TryParse("99 0 0 E",out value));
        Assert.Equal(99,value);
        Assert.True(GeoPointLongitude.TryParse("100 0 0 E",out value));
        Assert.Equal(100,value);
        Assert.True(GeoPointLongitude.TryParse("179 0 0 E",out value));
        Assert.Equal(179,value);
        Assert.True(GeoPointLongitude.TryParse("79 0 0 E",out value));
        Assert.Equal(79,value);
        Assert.True(GeoPointLongitude.TryParse("180 0 0 E",out value));
        Assert.Equal(180,value);
        Assert.True(GeoPointLongitude.TryParse("001° 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("01° 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("1° 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("10° 0 0 E",out value));
        Assert.Equal(10,value);
        Assert.True(GeoPointLongitude.TryParse("99° 0 0 E",out value));
        Assert.Equal(99,value);
        Assert.True(GeoPointLongitude.TryParse("100° 0 0 E",out value));
        Assert.Equal(100,value);
        Assert.True(GeoPointLongitude.TryParse("179° 0 0 E",out value));
        Assert.Equal(179,value);
        Assert.True(GeoPointLongitude.TryParse("79° 0 0 E",out value));
        Assert.Equal(79,value);
        Assert.True(GeoPointLongitude.TryParse("180° 0 0 E",out value));
        Assert.Equal(180,value);
        Assert.True(GeoPointLongitude.TryParse("001˚ 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("01˚ 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("1˚ 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("10˚ 0 0 E",out value));
        Assert.Equal(10,value);
        Assert.True(GeoPointLongitude.TryParse("99˚ 0 0 E",out value));
        Assert.Equal(99,value);
        Assert.True(GeoPointLongitude.TryParse("100˚ 0 0 E",out value));
        Assert.Equal(100,value);
        Assert.True(GeoPointLongitude.TryParse("179˚ 0 0 E",out value));
        Assert.Equal(179,value);
        Assert.True(GeoPointLongitude.TryParse("79˚ 0 0 E",out value));
        Assert.Equal(79,value);
        Assert.True(GeoPointLongitude.TryParse("180˚ 0 0 E",out value));
        Assert.Equal(180,value);
        Assert.True(GeoPointLongitude.TryParse("001º 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("01º 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("1º 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("10º 0 0 E",out value));
        Assert.Equal(10,value);
        Assert.True(GeoPointLongitude.TryParse("99º 0 0 E",out value));
        Assert.Equal(99,value);
        Assert.True(GeoPointLongitude.TryParse("100º 0 0 E",out value));
        Assert.Equal(100,value);
        Assert.True(GeoPointLongitude.TryParse("179º 0 0 E",out value));
        Assert.Equal(179,value);
        Assert.True(GeoPointLongitude.TryParse("79º 0 0 E",out value));
        Assert.Equal(79,value);
        Assert.True(GeoPointLongitude.TryParse("180º 0 0 E",out value));
        Assert.Equal(180,value);
        Assert.True(GeoPointLongitude.TryParse("001^ 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("01^ 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("1^ 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("10^ 0 0 E",out value));
        Assert.Equal(10,value);
        Assert.True(GeoPointLongitude.TryParse("99^ 0 0 E",out value));
        Assert.Equal(99,value);
        Assert.True(GeoPointLongitude.TryParse("100^ 0 0 E",out value));
        Assert.Equal(100,value);
        Assert.True(GeoPointLongitude.TryParse("179^ 0 0 E",out value));
        Assert.Equal(179,value);
        Assert.True(GeoPointLongitude.TryParse("79^ 0 0 E",out value));
        Assert.Equal(79,value);
        Assert.True(GeoPointLongitude.TryParse("180^ 0 0 E",out value));
        Assert.Equal(180,value);
        Assert.True(GeoPointLongitude.TryParse("001~ 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("01~ 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("1~ 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("10~ 0 0 E",out value));
        Assert.Equal(10,value);
        Assert.True(GeoPointLongitude.TryParse("99~ 0 0 E",out value));
        Assert.Equal(99,value);
        Assert.True(GeoPointLongitude.TryParse("100~ 0 0 E",out value));
        Assert.Equal(100,value);
        Assert.True(GeoPointLongitude.TryParse("179~ 0 0 E",out value));
        Assert.Equal(179,value);
        Assert.True(GeoPointLongitude.TryParse("79~ 0 0 E",out value));
        Assert.Equal(79,value);
        Assert.True(GeoPointLongitude.TryParse("180~ 0 0 E",out value));
        Assert.Equal(180,value);
        Assert.True(GeoPointLongitude.TryParse("001* 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("01* 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("1* 0 0 E",out value));
        Assert.Equal(1,value);
        Assert.True(GeoPointLongitude.TryParse("10* 0 0 E",out value));
        Assert.Equal(10,value);
        Assert.True(GeoPointLongitude.TryParse("99* 0 0 E",out value));
        Assert.Equal(99,value);
        Assert.True(GeoPointLongitude.TryParse("100* 0 0 E",out value));
        Assert.Equal(100,value);
        Assert.True(GeoPointLongitude.TryParse("179* 0 0 E",out value));
        Assert.Equal(179,value);
        Assert.True(GeoPointLongitude.TryParse("79* 0 0 E",out value));
        Assert.Equal(79,value);
        Assert.True(GeoPointLongitude.TryParse("180* 0 0 E",out value));
        Assert.Equal(180,value);
    }

    [Fact]
    public void CheckValidLongitudeMinuteValues()
    {
        var value = 0.0;
        Assert.True(GeoPointLongitude.TryParse("000 01 0 E",out value));
        Assert.Equal(1.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 1 0 E",out value));
        Assert.Equal(1.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 9 0 E",out value));
        Assert.Equal(9.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 09 0 E",out value));
        Assert.Equal(9.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 10 0 E",out value));
        Assert.Equal(10.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 59 0 E",out value));
        Assert.Equal(59.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 5 0 E",out value));
        Assert.Equal(5.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 01' 0 E",out value));
        Assert.Equal(1.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 1' 0 E",out value));
        Assert.Equal(1.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 9' 0 E",out value));
        Assert.Equal(9.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 09' 0 E",out value));
        Assert.Equal(9.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 10' 0 E",out value));
        Assert.Equal(10.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 59' 0 E",out value));
        Assert.Equal(59.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 5' 0 E",out value));
        Assert.Equal(5.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 01′ 0 E",out value));
        Assert.Equal(1.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 1′ 0 E",out value));
        Assert.Equal(1.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 9′ 0 E",out value));
        Assert.Equal(9.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 09′ 0 E",out value));
        Assert.Equal(9.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 10′ 0 E",out value));
        Assert.Equal(10.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 59′ 0 E",out value));
        Assert.Equal(59.0/60.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 5′ 0 E",out value));
        Assert.Equal(5.0/60.0,value);
    }
    
    [Fact]
    public void CheckValidLongitudeSecondValues()
    {
        var value = 0.0;
        Assert.True(GeoPointLongitude.TryParse("000 00 01 E",out value));
        Assert.Equal(1.0/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 00 1 E",out value));
        Assert.Equal(1.0/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 00 09 E",out value));
        Assert.Equal(9.0/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 00 9 E",out value));
        Assert.Equal(9.0/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 00 59 E",out value));
        Assert.Equal(59.0/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 00 01.001 E",out value));
        Assert.Equal(1.001/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 00 1.001 E",out value));
        Assert.Equal(1.001/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 00 09.001 E",out value));
        Assert.Equal(9.001/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 00 9.001 E",out value));
        Assert.Equal(9.001/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 00 59.001 E",out value));
        Assert.Equal(59.001/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("0000001E",out value));
        Assert.Equal(1.0/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("000001E",out value));
        Assert.Equal(1.0/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("0000009E",out value));
        Assert.Equal(9.0/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("000009E",out value));
        Assert.Equal(9.0/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("0000059E",out value));
        Assert.Equal(59.0/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("0000001.001E",out value));
        Assert.Equal(1.001/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 00 1.001 E",out value));
        Assert.Equal(1.001/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 00 09.001 E",out value));
        Assert.Equal(9.001/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 00 9.001 E",out value));
        Assert.Equal(9.001/3600.0,value);
        Assert.True(GeoPointLongitude.TryParse("000 00 59.001 E",out value));
        Assert.Equal(59.001/3600.0,value);
        
    }
}