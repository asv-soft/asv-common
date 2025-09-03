using Xunit;

namespace Asv.Common.Test.Other;

public class FloatingPointComparerTest
{
    [Theory]
    [InlineData(-0f, -0f)]
    [InlineData(1f, 1f)]
    [InlineData(0.01f, 0.01f)]
    [InlineData(0.000000004f, 0.000000008f)]
    [InlineData(float.Epsilon, 0f)]
    [InlineData(0f, float.Epsilon)]
    [InlineData(float.MinValue, float.MinValue)]
    [InlineData(float.MaxValue, float.MaxValue)]
    [InlineData(float.NaN, float.NaN)]
    [InlineData(float.NegativeInfinity, float.NegativeInfinity)]
    [InlineData(float.PositiveInfinity, float.PositiveInfinity)]
    public void ApproximatelyEquals_WithFloats_ShouldBeEquals(float first, float second)
    {
        // Act
        var result = first.ApproximatelyEquals(second);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(1f, -1f)]
    [InlineData(0.01f, 0.02f)]
    [InlineData(0.00000004f, 0.00000008f)]
    [InlineData(float.Epsilon, 0.03f)]
    [InlineData(0.03f, float.Epsilon)]
    [InlineData(float.MinValue, float.MaxValue)]
    [InlineData(float.MaxValue, float.MinValue)]
    [InlineData(float.NaN, float.Epsilon)]
    [InlineData(float.NegativeInfinity, float.PositiveInfinity)]
    [InlineData(float.PositiveInfinity, float.NegativeInfinity)]
    [InlineData(float.PositiveInfinity, 0.1f)]
    [InlineData(float.NegativeInfinity, 0.1f)]
    public void ApproximatelyNotEquals_WithFloats_ShouldNotBeEquals(float first, float second)
    {
        // Act
        var result = first.ApproximatelyNotEquals(second);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(0.001f, 0.003f, 0.01f)]
    [InlineData(float.Epsilon, 0f, 0.01f)]
    [InlineData(float.MaxValue, float.MaxValue, 0.01f)]
    [InlineData(float.Epsilon, float.Epsilon, 0.01f)]
    public void ApproximatelyEquals_WithFloatsAndCustomEpsilon_ShouldBeEquals(
        float first, float second, float epsilon)
    {
        // Act
        var result = first.ApproximatelyEquals(second, epsilon);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(0.01f, 0.03f, 0.01f)]
    [InlineData(float.Epsilon, 0.03f, 0.01f)]
    [InlineData(float.MinValue, float.MaxValue, 0.01f)]
    [InlineData(0.000001f, 0.000002f, float.Epsilon)]
    public void ApproximatelyNotEquals_WithFloatsAndCustomEpsilon_ShouldNotBeEquals(
        float first, float second, float epsilon)
    {
        // Act
        var result = first.ApproximatelyNotEquals(second, epsilon);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(0.000000004, 0.000000003)]
    [InlineData(0.0, 1e-12)]
    [InlineData(1e-200, 1.0000000005e-200)]
    [InlineData(double.Epsilon, 0.0)]
    [InlineData(double.MinValue, double.MinValue)]
    [InlineData(double.NaN, double.NaN)]
    [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
    public void ApproximatelyEquals_WithDouble_ShouldBeEqual(double first, double second)
    {
        // Act
        var result = first.ApproximatelyEquals(second);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(0.00000004, 0.00000003)]
    [InlineData(double.Epsilon, 0.001)]
    [InlineData(double.MinValue, double.MaxValue)]
    [InlineData(double.NaN, double.Epsilon)]
    [InlineData(double.NegativeInfinity, double.PositiveInfinity)]
    [InlineData(double.PositiveInfinity, 0.1)]
    [InlineData(double.NegativeInfinity, 0.1)]
    public void ApproximatelyNotEquals_WithDouble_ShouldNotBeEqual(double first, double second)
    {
        // Act
        var result = first.ApproximatelyNotEquals(second);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(0.001, 0.003, 0.01)]
    [InlineData(double.Epsilon, 0, 0.01)]
    [InlineData(double.MaxValue, double.MaxValue, 0.01)]
    [InlineData(double.Epsilon, double.Epsilon, 0.1)]
    public void ApproximatelyEquals_WithDoubleAndCustomEpsilon_ShouldBeEqual(
        double first, double second, double epsilon)
    {
        // Act
        var result = first.ApproximatelyEquals(second, epsilon);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(0.01, 0.03, 0.01)]
    [InlineData(double.Epsilon, 0.03, 0.01)]
    [InlineData(double.MinValue, double.MaxValue, 0.01)]
    [InlineData(0.00000004, 0.00000008, double.Epsilon)]
    public void ApproximatelyNotEquals_WithDoubleAndCustomEpsilon_ShouldNotBeEqual(
        double first, double second, double epsilon)
    {
        // Act
        var result = first.ApproximatelyNotEquals(second, epsilon);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(0.01, 0.01)]
    [InlineData(0, 0.000000002)]
    public void ApproximatelyEquals_WithDecimals_ShouldBeEquals(decimal first, decimal second)
    {
        // Act
        var result = first.ApproximatelyEquals(second);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(0.01, 0.02)]
    [InlineData(0, 0.00000002)]
    public void ApproximatelyNotEquals_WithDecimals_ShouldNotBeEquals(decimal first, decimal second)
    {
        // Act
        var result = first.ApproximatelyNotEquals(second);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ApproximatelyEquals_WithDecimalsAndCustomEpsilon_ShouldBeEquals()
    {
        // Arrange
        const decimal first = 0.001m;
        const decimal second = 0.003m;
        const decimal epsilon = 0.01m;

        // Act
        var result = first.ApproximatelyEquals(second, epsilon);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ApproximatelyNotEquals_WithDecimalsAndCustomEpsilon_ShouldNotBeEquals()
    {
        // Arrange
        const decimal first = 0.01m;
        const decimal second = 0.03m;
        const decimal epsilon = 0.01m;

        // Act
        var result = first.ApproximatelyNotEquals(second, epsilon);

        // Assert
        Assert.True(result);
    }
}