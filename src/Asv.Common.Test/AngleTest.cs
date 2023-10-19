using System;
using Xunit;

namespace Asv.Common.Test;

public class AngleTest
{
    [Fact]
    public void Check_double_values()
    {
        var value = 0.0;
        Assert.True(Angle.TryParse("2.40",out value));
        Assert.Equal(2.40,value);
        
        Assert.True(Angle.TryParse("-3.40",out value));
        Assert.Equal(-3.40,value);
    }

    [Fact]
    public void CheckPlusAndMinus()
    {
        var value = 0.0;
        Assert.True(Angle.TryParse("-0 0 0",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse("+0 0 0",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse(" -0 0 0",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse(" +0 0 0",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse(" 0 0 0",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse("0 0 0 ",out value));
        Assert.Equal(0,value);
    }
    
    [Fact]
    public void CheckDegreeSymbols()
    {
        var value = 0.0;
        Assert.True(Angle.TryParse(@"0° 0' 0""",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse(@"0˚ 0' 0""",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse(@"0º 0' 0""",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse(@"0^ 0' 0""",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse(@"0~ 0' 0""",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse(@"0* 0' 0""",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse(@"0 0' 0""",out value));
        Assert.Equal(0,value);
    }
    
    [Fact]
    public void CheckMinuteSymbols()
    {
        var value = 0.0;
        Assert.True(Angle.TryParse(@"000° 00' 00""",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse(@"000° 00' 00 ",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse(@"000° 00' 00"" ",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse(@"000° 00′ 00""",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse(@"000° 00′ 00 """,out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse(@"000° 00′ 00"" ",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse(@"000° 00′ 00 ",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse(@"000° 00' 00 """,out value));
        Assert.Equal(0,value);
    }
    
    [Fact]
    public void CheckFullDmsWithDifferingSpaces()
    {
        var value = 0.0;
        Assert.True(Angle.TryParse("0°00'00˝",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse("0 °00'00˝",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse("0 ° 00'00˝",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse("0 ° 00 '00˝",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse("0 ° 00 ' 00˝",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse("0 ° 00 ' 00 ˝",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse("0 ° 00 ' 00 ˝ ",out value));
        Assert.Equal(0,value);
    }
    
    [Fact]
    public void CheckIncompleteEntries()
    {
        var value = 0.0;
        Assert.True(Angle.TryParse("0 0 0",out value));
        Assert.Equal(0,value);
        Assert.True(Angle.TryParse("2 40",out value));
        Assert.Equal(2 + 40.0 / 60.0,value);
        Assert.Equal("02°40′00,00˝", Angle.PrintDms(value));
        Assert.True(Angle.TryParse("0",out value));
        Assert.Equal(0,value);
    }
    
    [Fact]
    public void CheckValidAngleDegValues()
    {
        var value = 0.0;
        Assert.True(Angle.TryParse("+045 0 0",out value));
        Assert.Equal(45,value);
        Assert.True(Angle.TryParse("-090 0 0",out value));
        Assert.Equal(-90,value);
        Assert.True(Angle.TryParse("060 0 0",out value));
        Assert.Equal(60,value);
        Assert.True(Angle.TryParse("180 0 0",out value));
        Assert.Equal(180,value);
        Assert.True(Angle.TryParse("089 0 0",out value));
        Assert.Equal(89,value);
        Assert.True(Angle.TryParse("-289 0 0",out value));
        Assert.Equal(-289,value);
        Assert.True(Angle.TryParse("-054 0 0",out value));
        Assert.Equal(-54,value);
        
        Assert.True(Angle.TryParse("+045 0",out value));
        Assert.Equal(45,value);
        Assert.True(Angle.TryParse("-090 0",out value));
        Assert.Equal(-90,value);
        Assert.True(Angle.TryParse("060 0",out value));
        Assert.Equal(60,value);
        Assert.True(Angle.TryParse("180 0",out value));
        Assert.Equal(180,value);
        Assert.True(Angle.TryParse("089 0",out value));
        Assert.Equal(89,value);
        Assert.True(Angle.TryParse("-289 0",out value));
        Assert.Equal(-289,value);
        Assert.True(Angle.TryParse("-054 0",out value));
        Assert.Equal(-54,value);
        
        Assert.True(Angle.TryParse("+045",out value));
        Assert.Equal(45,value);
        Assert.True(Angle.TryParse("-090",out value));
        Assert.Equal(-90,value);
        Assert.True(Angle.TryParse("060",out value));
        Assert.Equal(60,value);
        Assert.True(Angle.TryParse("180",out value));
        Assert.Equal(180,value);
        Assert.True(Angle.TryParse("089",out value));
        Assert.Equal(89,value);
        Assert.True(Angle.TryParse("-289",out value));
        Assert.Equal(-289,value);
        Assert.True(Angle.TryParse("-054",out value));
        Assert.Equal(-54,value);
        Assert.True(Angle.TryParse("001054000",out value));
        Assert.Equal(1054000,value);
    }
    
    [Fact]
    public void CheckValidAngleMinuteValues()
    {
        var value = 0.0;
        Assert.True(Angle.TryParse("00 30 00",out value));
        Assert.Equal(30.0/60.0,value);
        Assert.True(Angle.TryParse("00 1 00",out value));
        Assert.Equal(1.0/60.0,value);
        Assert.True(Angle.TryParse("00 09 00",out value));
        Assert.Equal(9.0/60.0,value);
        Assert.True(Angle.TryParse("00 9 00",out value));
        Assert.Equal(9.0/60.0,value);
        Assert.True(Angle.TryParse("00 59 00",out value));
        Assert.Equal(59.0/60.0,value);
        
        Assert.True(Angle.TryParse("00 30",out value));
        Assert.Equal(30.0/60.0,value);
        Assert.True(Angle.TryParse("-00 1",out value));
        Assert.Equal(-1.0/60.0,value);
        Assert.True(Angle.TryParse("00 09",out value));
        Assert.Equal(9.0/60.0,value);
        Assert.True(Angle.TryParse("-00 9",out value));
        Assert.Equal(-9.0/60.0,value);
        Assert.True(Angle.TryParse("00 59",out value));
        Assert.Equal(59.0/60.0,value);
    }
    
    [Fact]
    public void CheckValidAngleSecondValues()
    {
        var value = 0.0;
        Assert.True(Angle.TryParse("00 00 01",out value));
        Assert.Equal(1.0/3600.0,value);
        Assert.True(Angle.TryParse("-00 00 1",out value));
        Assert.Equal(-1.0/3600.0,value);
        Assert.True(Angle.TryParse("00 00 09",out value));
        Assert.Equal(9.0/3600.0,value);
        Assert.True(Angle.TryParse("-00 00 9",out value));
        Assert.Equal(-9.0/3600.0,value);
        Assert.True(Angle.TryParse("00 00 59",out value));
        Assert.Equal(59.0/3600.0,value);
        Assert.True(Angle.TryParse("-00 00 01.001",out value));
        Assert.Equal(-1.001/3600.0,value);
        Assert.True(Angle.TryParse("00 00 1.001",out value));
        Assert.Equal(1.001/3600.0,value);
        Assert.True(Angle.TryParse("-00 00 09.001",out value));
        Assert.Equal(-9.001/3600.0,value);
        Assert.True(Angle.TryParse("00 00 9.001",out value));
        Assert.Equal(9.001/3600.0,value);
        Assert.True(Angle.TryParse("-00 00 59.001",out value));
        Assert.Equal(-59.001/3600.0,value);
    }
}