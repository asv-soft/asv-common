using Xunit;

namespace Asv.Common.Test;

public class GeoPointLatitudeTest
{
    [Fact]
    public void ZeroTest()
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
        Assert.True(GeoPointLatitude.TryParse("0 0 0",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 0 0 ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 0 0",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("+0 0 0 ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 0 0+",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 0 0+",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse("0 0 0",out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckMinuteSymbols()
    {
        var value = 0.0;
        Assert.True(GeoPointLatitude.TryParse(@"000° 00' 00""",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00' 00""",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00' 00 ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00' 00"" ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00′ 00""",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00′ 00""",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00′ 00 """,out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00′ 00"" ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00′ 00""",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00′ 00""",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00′ 00 ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00′ 00"" ",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00' 00""",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00' 00""",out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00' 00 """,out value));
        Assert.Equal(0,value);
        Assert.True(GeoPointLatitude.TryParse(@"000° 00' 00"" ",out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckSecondSymbols()
    {
        var value = 0.0;
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00\"",out value));
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00\" N",out value));
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00.000\" N",out value));
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00\"",out value));
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00¨ N",out value));
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00¨ ",out value));
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00.000¨ N",out value));
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00¨ n",out value));
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00˝ n",out value));
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00˝ ",out value));
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00.000˝ n",out value));
        Assert.True(GeoPointLatitude.TryParse("000° 00' 00˝ n",out value));
    }
}