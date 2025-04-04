using Asv.Common;
using JetBrains.Annotations;
using Xunit;

namespace Asv.Common.Test.Other;

[TestSubject(typeof(DigitHelper))]
public class DigitHelperTest
{

    [Theory]
    [InlineData(0, 1)]
    [InlineData(9, 1)]
    [InlineData(10, 2)]
    [InlineData(12345, 5)]
    [InlineData(123456789, 9)]
    public void CountDigits_ShouldReturnCorrectUintDigitCount(uint input, int expectedDigits)
    {
        var digits = input.CountDecDigits();
        Assert.Equal(expectedDigits, digits);
    }
    
    [Theory]
    [InlineData(0, 1)]
    [InlineData(9, 1)]
    [InlineData(10, 2)]
    [InlineData(12345, 5)]
    [InlineData(123456789, 9)]
    [InlineData(-1234, 5)]
    public void CountDigits_ShouldReturnCorrectIntDigitCount(int input, int expectedDigits)
    {
        var digits = input.CountDecDigits();
        Assert.Equal(expectedDigits, digits);
    }

    [Theory]
    [InlineData(0u, 1)]
    [InlineData(0xF, 1)]
    [InlineData(0x10, 2)]
    [InlineData(0xFF, 2)]
    [InlineData(0x100, 3)]
    [InlineData(0xFFF, 3)]
    [InlineData(0x1000, 4)]
    [InlineData(0xFFFF, 4)]
    [InlineData(0x10000, 5)]
    [InlineData(0xFFFFFFFF, 8)]
    public void CountHexDigits_UInt32_ReturnsCorrectDigits(uint value, int expectedDigits)
    {
        var result = value.CountHexDigits();
        Assert.Equal(expectedDigits, result);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(15, 1)]           // 0xF
    [InlineData(16, 2)]           // 0x10
    [InlineData(255, 2)]          // 0xFF
    [InlineData(4096, 4)]         // 0x1000
    [InlineData(int.MaxValue, 7)] // 0x7FFFFFFF
    [InlineData(-1, 8)]           // 0xFFFFFFFF (uint: 4294967295)
    [InlineData(-255, 8)]         // 0xFFFFFF01
    public void CountHexDigits_Int32_ReturnsCorrectDigits(int value, int expectedDigits)
    {
        var result = value.CountHexDigits();
        Assert.Equal(expectedDigits, result);
    }
}