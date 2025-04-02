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
        var digits = input.ToStringCharCount();
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
        var digits = input.CountDigits();
        Assert.Equal(expectedDigits, digits);
    }
}