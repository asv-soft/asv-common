using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Asv.Common;

/// <summary>
/// Creates localized display phrases from a characteristic, a color or material,
/// and an object. Each dictionary contains 256 entries (16,777,216 combinations).
/// These names are not unique identifiers or security checksums.
/// </summary>
public static class WordFingerprint
{
    /// <summary>
    /// Generates a phrase equivalent to hashing the GUID's lowercase canonical D representation.
    /// </summary>
    public static string Generate(Guid value, CultureInfo? culture = null)
    {
        return Generate(value.ToString("D"), culture);
    }

    /// <summary>
    /// Generates three words from the MD5 digest of the exact UTF-8 input.
    /// Russian phrases use masculine singular forms. Culture defaults to CurrentUICulture.
    /// </summary>
    /// <exception cref="ArgumentNullException">The input is null.</exception>
    public static string Generate(string value, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        culture ??= CultureInfo.CurrentUICulture;
        Span<byte> digest = stackalloc byte[16];
        MD5.HashData(Encoding.UTF8.GetBytes(value), digest);

        // Angry Purple Tiger folds the whole digest into segments of 5, 5, and 6 bytes.
        var characteristic = GetWord("Characteristics", Fold(digest[..5]), culture);
        var material = GetWord("Materials", Fold(digest[5..10]), culture);
        var objectName = GetWord("Objects", Fold(digest[10..]), culture);
        return string.Join(" ", characteristic, material, objectName);
    }

    private static byte Fold(ReadOnlySpan<byte> bytes)
    {
        byte result = 0;
        foreach (var value in bytes)
        {
            result ^= value;
        }

        return result;
    }

    private static string GetWord(string category, byte index, CultureInfo culture)
    {
        var key = "WordFingerprint_" + category + "_" + index.ToString("D3", CultureInfo.InvariantCulture);
        return RS.ResourceManager.GetString(key, culture)
            ?? throw new InvalidOperationException("Missing fingerprint vocabulary entry: " + key);
    }
}
