using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using Xunit;

namespace Asv.Common.Test.Naming;

public class WordFingerprintTest
{
    [Theory]
    [InlineData("", "loyal polyethylene plane", "верный полиэтиленовый платан")]
    [InlineData("station-01", "heroic porphyry spectrum", "героический порфировый спектр")]
    [InlineData("Привет 🌍", "timeless chrysocolla pebble", "вневременной хризоколловый камешек")]
    public void Generate_MatchesFrozenMd5Vectors(string input, string english, string russian)
    {
        Assert.Equal(english, WordFingerprint.Generate(input, CultureInfo.GetCultureInfo("en")));
        Assert.Equal(russian, WordFingerprint.Generate(input, CultureInfo.GetCultureInfo("ru")));
    }

    [Fact]
    public void Generate_GuidMatchesCanonicalString()
    {
        var id = Guid.Parse("c9a646d3-9c61-4cb7-bfcd-ee2522c8f633");
        var culture = CultureInfo.InvariantCulture;
        Assert.Equal("masterful linden sand", WordFingerprint.Generate(id, culture: culture));
        Assert.Equal(WordFingerprint.Generate(id.ToString("D"), culture: culture), WordFingerprint.Generate(id, culture: culture));
        Assert.Equal(3, WordFingerprint.Generate(Guid.Empty).Split(' ').Length);
    }

    [Fact]
    public void Generate_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => WordFingerprint.Generate(null!));
    }

    [Fact]
    public void Generate_UsesUiCultureAndParentFallback()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ru-RU");
            Assert.Equal("героический порфировый спектр", WordFingerprint.Generate("station-01"));
            Assert.Equal("heroic porphyry spectrum", WordFingerprint.Generate("station-01", culture: CultureInfo.GetCultureInfo("fr-FR")));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Theory]
    [InlineData("en")]
    [InlineData("ru")]
    public void Vocabulary_HasThreeCompleteDictionariesAndNoLegacyEntries(string language)
    {
        var manager = new ResourceManager("Asv.Common.RS", typeof(WordFingerprint).Assembly);
        var culture = CultureInfo.GetCultureInfo(language);
        var expectedKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var category in new[] { "Characteristics", "Materials", "Objects" })
        {
            var words = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < 256; index++)
            {
                var key = "WordFingerprint_" + category + "_" + index.ToString("D3", CultureInfo.InvariantCulture);
                expectedKeys.Add(key);
                var value = manager.GetString(key, culture);
                Assert.NotNull(value);
                Assert.True(value.Length > 0 && value.All(char.IsLetter), key);
                Assert.True(words.Add(value), "Duplicate vocabulary word: " + value);
                if (language == "ru" && category != "Objects")
                {
                    Assert.True(
                        value.EndsWith("ый", StringComparison.Ordinal)
                        || value.EndsWith("ий", StringComparison.Ordinal)
                        || value.EndsWith("ой", StringComparison.Ordinal)
                        || value.EndsWith("ийся", StringComparison.Ordinal),
                        key);
                }
            }

            Assert.Equal(256, words.Count);
        }

        var resources = manager.GetResourceSet(culture, true, true);
        Assert.NotNull(resources);
        var actualKeys = resources.Cast<DictionaryEntry>().Select(entry => (string)entry.Key)
            .Where(key => key.StartsWith("WordFingerprint_", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal);
        Assert.True(expectedKeys.SetEquals(actualKeys));
    }
}
