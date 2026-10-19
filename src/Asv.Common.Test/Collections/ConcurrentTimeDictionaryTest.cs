using System;
using Xunit;

namespace Asv.Common.Test.Collections;

public class ConcurrentTimeDictionaryTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetValues_CallbackThrows_ReleasesLock(bool keyExists)
    {
        using var dictionary = new ConcurrentTimeDictionary<string, string>(
            TimeSpan.FromMinutes(1), _ => DateTime.Now, DefaultTimeService.Default
        );
        if (keyExists)
        {
            dictionary.AddOrUpdate("key", () => "before", _ => { });
        }
        var expected = new InvalidOperationException("Callback failed.");

        var actual = Assert.Throws<InvalidOperationException>(() =>
            dictionary.GetValues<string>("key", _ => throw expected, () => throw expected)
        );

        Assert.Same(expected, actual);
        dictionary.GetKeys();
        dictionary.AddOrUpdate("after", () => "value", _ => { });
        Assert.Equal("value", dictionary.GetValues("after", value => value, () => "missing"));
    }

    [Fact]
    public void ClearOld_CallbackThrows_ReleasesLock()
    {
        var shouldThrow = true;
        var expected = new InvalidOperationException("Timestamp callback failed.");
        using var dictionary = new ConcurrentTimeDictionary<string, string>(
            TimeSpan.FromMinutes(1),
            _ => shouldThrow ? throw expected : DateTime.Now.AddHours(-1),
            DefaultTimeService.Default
        );
        dictionary.AddOrUpdate("key", () => "value", _ => { });

        Assert.Same(expected, Assert.Throws<InvalidOperationException>(dictionary.ClearOld));
        shouldThrow = false;
        dictionary.ClearOld();

        Assert.Empty(dictionary.GetKeys());
        dictionary.AddOrUpdate("after", () => "value", _ => { });
        Assert.Equal("value", dictionary.GetValues("after", value => value, () => "missing"));
    }
}
