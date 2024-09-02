using Xunit;

namespace Asv.Common.Test;

public class AngleDmTest
{
    [Fact]
    public void Check_double_values()
    {
        var value = 0.0;
        Assert.True(AngleDm.TryParse("2.40",out value));
        Assert.Equal(2.40,value);
        
        Assert.True(AngleDm.TryParse("-3.40",out value));
        Assert.Equal(-3.40,value);
        
        Assert.True(AngleDm.TryParse("-0.410",out value));
        Assert.Equal(-0.41,value);
        
        Assert.True(AngleDm.TryParse("0.410",out value));
        Assert.Equal(0.41,value);
    }
    
    [Fact]
    public void CheckPlusAndMinus()
    {
        var value = 0.0;
        Assert.True(AngleDm.TryParse("-0 0",out value));
        Assert.Equal(0,value);
        Assert.True(AngleDm.TryParse("+0 0",out value));
        Assert.Equal(0,value);
        Assert.True(AngleDm.TryParse(" -0 0",out value));
        Assert.Equal(0,value);
        Assert.True(AngleDm.TryParse(" +0 0",out value));
        Assert.Equal(0,value);
        Assert.True(AngleDm.TryParse(" 0 0",out value));
        Assert.Equal(0,value);
        Assert.True(AngleDm.TryParse("0 0 ",out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckDegreeSymbols()
    {
        var value = 0.0;
        Assert.True(AngleDm.TryParse(@"0° 0'",out value));
        Assert.Equal(0,value);
        Assert.True(AngleDm.TryParse(@"0˚ 0'",out value));
        Assert.Equal(0,value);
        Assert.True(AngleDm.TryParse(@"0º 0'",out value));
        Assert.Equal(0,value);
        Assert.True(AngleDm.TryParse(@"0^ 0'",out value));
        Assert.Equal(0,value);
        Assert.True(AngleDm.TryParse(@"0~ 0'",out value));
        Assert.Equal(0,value);
        Assert.True(AngleDm.TryParse(@"0* 0'",out value));
        Assert.Equal(0,value);
        Assert.True(AngleDm.TryParse(@"0 0'",out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckMinuteSymbols()
    {
        var value = 0.0;
        Assert.True(AngleDm.TryParse(@"000° 00'", out value));
        Assert.Equal(0, value);
        Assert.True(AngleDm.TryParse(@"000° 00'", out value));
        Assert.Equal(0, value);
        Assert.True(AngleDm.TryParse(@"000° 00'", out value));
        Assert.Equal(0, value);
        Assert.True(AngleDm.TryParse(@"000° 00′", out value));
        Assert.Equal(0, value);
        Assert.True(AngleDm.TryParse(@"000° 00′", out value));
        Assert.Equal(0, value);
        Assert.True(AngleDm.TryParse(@"000° 00′", out value));
        Assert.Equal(0, value);
        Assert.True(AngleDm.TryParse(@"000° 00′", out value));
        Assert.Equal(0, value);
        Assert.True(AngleDm.TryParse(@"000° 00'", out value));
        Assert.Equal(0, value);
    }
    
    [Fact]
    public void CheckValidAngleDegValues()
    {
        var value = 0.0;
        Assert.True(AngleDm.TryParse("+045 0",out value));
        Assert.Equal(45,value);
        Assert.True(AngleDm.TryParse("-090 0",out value));
        Assert.Equal(-90,value);
        Assert.True(AngleDm.TryParse("060 0",out value));
        Assert.Equal(60,value);
        Assert.True(AngleDm.TryParse("180 0",out value));
        Assert.Equal(180,value);
        Assert.True(AngleDm.TryParse("089 0",out value));
        Assert.Equal(89,value);
        Assert.True(AngleDm.TryParse("-289 0",out value));
        Assert.Equal(-289,value);
        Assert.True(AngleDm.TryParse("-054 0",out value));
        Assert.Equal(-54,value);
        
        Assert.True(AngleDm.TryParse("+045",out value));
        Assert.Equal(45,value);
        Assert.True(AngleDm.TryParse("-090",out value));
        Assert.Equal(-90,value);
        Assert.True(AngleDm.TryParse("060",out value));
        Assert.Equal(60,value);
        Assert.True(AngleDm.TryParse("180",out value));
        Assert.Equal(180,value);
        Assert.True(AngleDm.TryParse("089",out value));
        Assert.Equal(89,value);
        Assert.True(AngleDm.TryParse("-289",out value));
        Assert.Equal(-289,value);
        Assert.True(AngleDm.TryParse("-054",out value));
        Assert.Equal(-54,value);
        Assert.True(AngleDm.TryParse("001054000",out value));
        Assert.Equal(1054000,value);
    }
    
    [Fact]
    public void CheckValidAngleMinuteValues()
    {
        var value = 0.0;
        Assert.True(AngleDm.TryParse("00 30",out value));
        Assert.Equal(30.0/60.0,value);
        Assert.Equal($"00°30,00′", AngleDm.PrintDm(value));
        Assert.True(AngleDm.TryParse("00 1",out value));
        Assert.Equal(1.0/60.0,value);
        Assert.Equal($"00°01,00′", AngleDm.PrintDm(value));
        Assert.True(AngleDm.TryParse("00 09",out value));
        Assert.Equal(9.0/60.0,value);
        Assert.Equal($"00°09,00′", AngleDm.PrintDm(value));
        Assert.True(AngleDm.TryParse("00 9",out value));
        Assert.Equal(9.0/60.0,value);
        Assert.Equal($"00°09,00′", AngleDm.PrintDm(value));
        Assert.True(AngleDm.TryParse("00 59",out value));
        Assert.Equal(59.0/60.0,value);
        Assert.Equal($"00°59,00′", AngleDm.PrintDm(value));
        
        Assert.True(AngleDm.TryParse("00 30.24",out value));
        Assert.Equal(30.24/60.0,value);
        Assert.Equal($"00°30,24′", AngleDm.PrintDm(value));
        Assert.True(AngleDm.TryParse("-00 1.12345",out value));
        Assert.Equal(-1.12345/60.0,value);
        Assert.Equal($"-00°01,12′", AngleDm.PrintDm(value));
        Assert.True(AngleDm.TryParse("00 09.999",out value));
        Assert.Equal(9.999/60.0,value);
        Assert.Equal($"00°10,00′", AngleDm.PrintDm(value));
        Assert.True(AngleDm.TryParse("-00 9.11",out value));
        Assert.Equal(-9.11/60.0,value);
        Assert.Equal($"-00°09,11′", AngleDm.PrintDm(value));
        Assert.True(AngleDm.TryParse("00 59.99",out value));
        Assert.Equal(59.99/60.0,value);
        Assert.Equal($"00°59,99′", AngleDm.PrintDm(value));
        Assert.True(AngleDm.TryParse("00 59.999",out value));
        Assert.Equal(59.999/60.0,value);
        Assert.Equal($"01°00,00′", AngleDm.PrintDm(value));
    }
}