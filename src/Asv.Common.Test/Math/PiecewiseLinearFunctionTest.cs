using System.Linq;
using Xunit;

namespace Asv.Common.Test.Math;

public class PiecewiseLinearFunctionTest
{
    [Fact]
    public void TestEmptyFunction()
    {
        double[,] values = new double[0, 0];
        PiecewiseLinearFunction function = new PiecewiseLinearFunction(values);

        double result = function[1.0]; // Should return 1.0 since it's an empty function
        Assert.Equal(1.0, result, 3); // Use an appropriate tolerance for floating-point comparisons
    }

    [Fact]
    public void TestLinearFunction()
    {
        double[,] values = new double[,]
        {
            { 0.0, 0.0 },
            { 1.0, 1.0 },
        };
        PiecewiseLinearFunction function = new PiecewiseLinearFunction(values);

        double result = function[0.5]; // Should return 0.5 for a linear function
        Assert.Equal(0.5, result, 3);
    }

    [Fact]
    public void TestLinearFunctionWithScale()
    {
        double[,] values = new double[,]
        {
            { 0.0, 0.0 },
            { 1.0, 2.0 },
        };
        PiecewiseLinearFunction function = new PiecewiseLinearFunction(
            values,
            isScaleForOnePoint: true
        );

        double result = function[0.5]; // Should return 1.0 because of scaling
        Assert.Equal(1.0, result, 3);
    }

    [Fact]
    public void TestLinearFunctionWithOffset()
    {
        double[,] values = new double[,]
        {
            { 0.0, 0.0 },
            { 1.0, 2.0 },
        };
        PiecewiseLinearFunction function = new PiecewiseLinearFunction(values);

        double result = function[0.5];
        Assert.Equal(1, result, 3);
    }

    [Fact]
    public void TestInterpolation()
    {
        double[,] values = new double[,]
        {
            { 0.0, 0.0 },
            { 1.0, 1.0 },
            { 2.0, 2.0 },
        };
        PiecewiseLinearFunction function = new PiecewiseLinearFunction(values);

        double result = function[1.5]; // Should return 1.5 (interpolated)
        Assert.Equal(1.5, result, 3);
    }

    [Fact]
    public void TestNaNInput()
    {
        double[,] values = new double[,]
        {
            { 0.0, 0.0 },
            { 1.0, 1.0 },
        };
        PiecewiseLinearFunction function = new PiecewiseLinearFunction(values);

        double result = function[double.NaN]; // Should return NaN for NaN input
        Assert.True(double.IsNaN(result));
    }

    [Fact]
    public void TestInfinityInput()
    {
        double[,] values = new double[,]
        {
            { 0.0, 0.0 },
            { 1.0, 1.0 },
        };
        PiecewiseLinearFunction function = new PiecewiseLinearFunction(values);

        double result = function[double.PositiveInfinity]; // Should return PositiveInfinity for Infinity input
        Assert.Equal(double.PositiveInfinity, result, 3);
    }

    [Fact]
    public void TestValueBeforeFirstPoint()
    {
        double[,] values = new double[,]
        {
            { 1.0, 1.0 },
            { 2.0, 2.0 },
        };
        PiecewiseLinearFunction function = new PiecewiseLinearFunction(values);

        double result = function[0.5]; // Should return 0.5 since it's before the first point
        Assert.Equal(0.5, result, 3);
    }

    [Fact]
    public void TestValueAfterLastPoint()
    {
        double[,] values = new double[,]
        {
            { 1.0, 1.0 },
            { 2.0, 2.0 },
        };
        PiecewiseLinearFunction function = new PiecewiseLinearFunction(values);

        double result = function[3.0]; // Should return 3.0 since it's after the last point
        Assert.Equal(3.0, result, 3);
    }

    [Fact]
    public void TestValueEnumerator()
    {
        double[,] values = new double[,]
        {
            { 1.0, 1.0 },
            { 2.0, 2.0 },
        };
        var function = new PiecewiseLinearFunction(values);

        var result = function.ToArray();
        Assert.Equal(2, result.Length);
        Assert.Equal(1.0, result[0].Key, 3);
        Assert.Equal(1.0, result[0].Value, 3);
        Assert.Equal(2.0, result[1].Key, 3);
        Assert.Equal(2.0, result[1].Value, 3);
    }
}
