using Xunit;

namespace Asv.Common.Test;

public class AngleMsTest
{
    [Fact]
    public void Check_double_values()
    {
        var value = 0.0;
        Assert.True(AngleMs.TryParse("2.40",out value));
        Assert.Equal(2.40,value);
        
        Assert.True(AngleMs.TryParse("-3.40",out value));
        Assert.Equal(-3.40,value);
        
        Assert.True(AngleMs.TryParse("-0.410",out value));
        Assert.Equal(-0.41,value);
        
        Assert.True(AngleMs.TryParse("0.410",out value));
        Assert.Equal(0.41,value);
    }
    
    [Fact]
    public void CheckPlusAndMinus()
    {
        var value = 0.0;
        Assert.True(AngleMs.TryParse("-0 0",out value));
        Assert.Equal(0,value);
        Assert.True(AngleMs.TryParse("+0 0",out value));
        Assert.Equal(0,value);
        Assert.True(AngleMs.TryParse(" -0 0",out value));
        Assert.Equal(0,value);
        Assert.True(AngleMs.TryParse(" +0 0",out value));
        Assert.Equal(0,value);
        Assert.True(AngleMs.TryParse(" 0 0",out value));
        Assert.Equal(0,value);
        Assert.True(AngleMs.TryParse("0 0 ",out value));
        Assert.Equal(0,value);
    }
    
    [Fact]
    public void CheckMinuteSymbols()
    {
        var value = 0.0;
        Assert.True(AngleMs.TryParse(@"00' 00""",out value));
        Assert.Equal(0,value);
        Assert.True(AngleMs.TryParse(@"00' 00 ",out value));
        Assert.Equal(0,value);
        Assert.True(AngleMs.TryParse(@"00' 00"" ",out value));
        Assert.Equal(0,value);
        Assert.True(AngleMs.TryParse(@"00′ 00""",out value));
        Assert.Equal(0,value);
        Assert.True(AngleMs.TryParse(@"00′ 00 """,out value));
        Assert.Equal(0,value);
        Assert.True(AngleMs.TryParse(@"00′ 00"" ",out value));
        Assert.Equal(0,value);
        Assert.True(AngleMs.TryParse(@"00′ 00 ",out value));
        Assert.Equal(0,value);
        Assert.True(AngleMs.TryParse(@"00' 00 """,out value));
        Assert.Equal(0,value);
    }

    [Fact]
    public void CheckValidAngleMsMinuteValues()
    {
        var value = 0.0;
        Assert.True(AngleMs.TryParse("30 00",out value));
        Assert.Equal(30.0/60.0,value);
        Assert.Equal("30′00,00˝", AngleMs.PrintMs(value));
        Assert.True(AngleMs.TryParse("1 00",out value));
        Assert.Equal(1.0/60.0,value);
        Assert.Equal("01′00,00˝", AngleMs.PrintMs(value));
        Assert.True(AngleMs.TryParse("09 00",out value));
        Assert.Equal(9.0/60.0,value);
        Assert.Equal("09′00,00˝", AngleMs.PrintMs(value));
        Assert.True(AngleMs.TryParse("9 00",out value));
        Assert.Equal(9.0/60.0,value);
        Assert.Equal("09′00,00˝", AngleMs.PrintMs(value));
        Assert.True(AngleMs.TryParse("59 00",out value));
        Assert.Equal(59.0/60.0,value);
        Assert.Equal("59′00,00˝", AngleMs.PrintMs(value));
        
        Assert.True(AngleMs.TryParse("120 30",out value));
        Assert.Equal(120.0/60.0 + 30.0/3600.0,value);
        Assert.Equal("120′30,00˝", AngleMs.PrintMs(value));
        Assert.True(AngleMs.TryParse("-92 1",out value));
        Assert.Equal(-92.0/60.0 - 1.0/3600.0,value);
        Assert.Equal("92′01,00˝", AngleMs.PrintMs(value));
        Assert.True(AngleMs.TryParse("10000 09.14",out value));
        Assert.Equal( 10000.0/60.0 + 9.14/3600.0,value);
        Assert.Equal("10000′09,14˝", AngleMs.PrintMs(value));
    }

    [Fact]
    public void CheckValidAngleMsSecondValues()
    {
        var value = 0.0;
        Assert.True(AngleMs.TryParse("00 01",out value));
        Assert.Equal(1.0/3600.0,value);
        Assert.True(AngleMs.TryParse("-00 1",out value));
        Assert.Equal(-1.0/3600.0,value);
        Assert.True(AngleMs.TryParse("00 09",out value));
        Assert.Equal(9.0/3600.0,value);
        Assert.True(AngleMs.TryParse("-00 9",out value));
        Assert.Equal(-9.0/3600.0,value);
        Assert.True(AngleMs.TryParse("00 59",out value));
        Assert.Equal(59.0/3600.0,value);
        Assert.True(AngleMs.TryParse("-00 01.001",out value));
        Assert.Equal(-1.001/3600.0,value);
        Assert.True(AngleMs.TryParse("00 1.001",out value));
        Assert.Equal(1.001/3600.0,value);
        Assert.True(AngleMs.TryParse("-00 09.001",out value));
        Assert.Equal(-9.001/3600.0,value);
        Assert.True(AngleMs.TryParse("00 9.001",out value));
        Assert.Equal(9.001/3600.0,value);
        Assert.True(AngleMs.TryParse("-00 59.001",out value));
        Assert.Equal(-59.001/3600.0,value);
    }
}