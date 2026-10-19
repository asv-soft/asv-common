using System;
using Xunit;

namespace Asv.Common.Test.Collections;

public class ConcurrentCircularTimeBufferTest
{
    [Theory]
    [InlineData(-1, new[] { 30, 20, 10 })]
    [InlineData(10, new[] { 30, 20 })]
    [InlineData(15, new[] { 30, 20 })]
    [InlineData(20, new[] { 30 })]
    [InlineData(30, new int[0])]
    [InlineData(40, new int[0])]
    public void GetItemsWithTimeMoreThen_SelectsStrictlyNewerItems(int threshold, int[] expected)
    {
        var epoch = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        using var buffer = new ConcurrentCircularTimeBuffer<int>(
            TimeSpan.FromMinutes(1), value => epoch.AddSeconds(value)
        );
        buffer.Push(20);
        buffer.Push(10);
        buffer.Push(30);

        Assert.Equal(expected, buffer.GetItemsWithTimeMoreThen(epoch.AddSeconds(threshold)));
        Assert.Equal(new[] { 10, 20, 30 }, buffer.GetAll());
    }

    [Fact]
    public void GetItemsWithTimeMoreThen_EmptyBuffer_ReturnsEmpty()
    {
        using var buffer = new ConcurrentCircularTimeBuffer<DateTime>(TimeSpan.FromMinutes(1), value => value);
        Assert.Empty(buffer.GetItemsWithTimeMoreThen(DateTime.MinValue));
    }
}
