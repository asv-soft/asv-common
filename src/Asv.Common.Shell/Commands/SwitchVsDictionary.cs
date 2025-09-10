using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;

namespace Asv.Common.Shell;

public class SwitchVsDictionary
{
    private ImmutableDictionary<int, string> _dictionary;

    [GlobalSetup]
    public void Setup()
    {
        _dictionary = ImmutableDictionary.CreateRange(
            [
                new KeyValuePair<int, string>(1, "One"),
                new KeyValuePair<int, string>(2, "Two"),
                new KeyValuePair<int, string>(3, "Three"),
            ]
        );
    }

    [Benchmark]
    public string UseSwitch()
    {
        int value = 2;
        return value switch
        {
            1 => "One",
            2 => "Two",
            3 => "Three",
            _ => "Default",
        };
    }

    [Benchmark]
    public string UseDictionary()
    {
        int value = 2;
        return CollectionExtensions.GetValueOrDefault(_dictionary, value, "Default");
    }
}
