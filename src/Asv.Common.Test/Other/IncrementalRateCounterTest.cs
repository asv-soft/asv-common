using System;
using Microsoft.Extensions.Time.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Asv.Common.Test;

public class IncrementalRateCounterTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public IncrementalRateCounterTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 10)]
    [InlineData(10, 1)]
    [InlineData(10, 10)]
    public void Calculate_UpdatesRateCorrectly(int movingAverageSize, long value)
    {
        // Arrange
        var timeProvider = new FakeTimeProvider(); // Initial timestamp
        var counter = new IncrementalRateCounter(
            movingAverageSize: movingAverageSize,
            timeProvider
        );

        double rate = 0;
        for (var i = 0; i < movingAverageSize; i++)
        {
            timeProvider.Advance(TimeSpan.FromSeconds(1)); // Advance by 1 second
            rate = counter.Calculate((i + 1) * value);

            if (i == movingAverageSize - 1)
            {
                // The rate should be equal 1 at the end of the loop
                Assert.Equal(value, rate);
            }
            else
            {
                _testOutputHelper.WriteLine($"rate {i}: {rate}");

                // The rate should be less than 1 during the loop
                Assert.True(value > rate);
            }
        }
        var lastValue = value * movingAverageSize;
        for (var i = 0; i < movingAverageSize; i++)
        {
            timeProvider.Advance(TimeSpan.FromSeconds(1)); // Advance by 1 second
            rate = counter.Calculate(lastValue);

            if (i == movingAverageSize - 1)
            {
                // The rate should be equal 1 at the end of the loop
                Assert.Equal(0, rate);
            }
            else
            {
                _testOutputHelper.WriteLine($"rate {i}: {rate}");

                // The rate should be less than 1 during the loop
                Assert.True(0 < rate);
            }
        }
    }
}
