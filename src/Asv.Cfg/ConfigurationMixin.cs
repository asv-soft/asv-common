using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Asv.Cfg;

/// <summary>
/// Provides helper methods for working with configuration stores.
/// </summary>
public static partial class ConfigurationMixin
{
    private const string FixedNameRegexString = @"^(?!\d)[\w$]+$";

    [GeneratedRegex(FixedNameRegexString, RegexOptions.Compiled)]
    private static partial Regex MyRegex();

    private static readonly Regex KeyRegex = MyRegex();

    /// <summary>
    /// Validates that a configuration key has a supported format.
    /// </summary>
    /// <param name="key">The configuration key to validate.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (KeyRegex.IsMatch(key) == false)
        {
            throw new ArgumentException($"Invalid key '{key}': must be {FixedNameRegexString}");
        }
    }

    /// <summary>
    /// Gets the default case-insensitive comparer used for configuration keys.
    /// </summary>
    public static IEqualityComparer<string> DefaultKeyComparer { get; } =
        StringComparer.InvariantCultureIgnoreCase;

    /// <summary>
    /// Gets a configuration value or stores and returns the supplied default value.
    /// </summary>
    /// <typeparam name="TPocoType">The configuration value type.</typeparam>
    /// <param name="src">The configuration reader.</param>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value used when the key is missing.</param>
    /// <returns>The configuration value.</returns>
    public static TPocoType Get<TPocoType>(
        this IConfigurationReader src,
        string key,
        TPocoType defaultValue
    )
    {
        return src.Get(key, new Lazy<TPocoType>(() => defaultValue));
    }

    /// <summary>
    /// Gets a configuration value or stores and returns a new default instance.
    /// </summary>
    /// <typeparam name="TPocoType">The configuration value type.</typeparam>
    /// <param name="src">The configuration reader.</param>
    /// <param name="key">The configuration key.</param>
    /// <returns>The configuration value.</returns>
    public static TPocoType Get<TPocoType>(this IConfigurationReader src, string key)
        where TPocoType : new()
    {
        return src.Get(key, new Lazy<TPocoType>(() => new TPocoType()));
    }

    /// <summary>
    /// Loads a typed configuration value, updates it, and saves it back.
    /// </summary>
    /// <typeparam name="TPocoType">The configuration value type.</typeparam>
    /// <param name="src">The configuration store.</param>
    /// <param name="updateCallback">The callback that updates the value.</param>
    public static void Update<TPocoType>(this IConfiguration src, Action<TPocoType> updateCallback)
        where TPocoType : new()
    {
        var value = src.Get<TPocoType>();
        updateCallback(value);
        src.Set(value);
    }

    /// <summary>
    /// Gets a typed configuration value using the type name as the key.
    /// </summary>
    /// <typeparam name="TPocoType">The configuration value type.</typeparam>
    /// <param name="src">The configuration reader.</param>
    /// <returns>The configuration value.</returns>
    public static TPocoType Get<TPocoType>(this IConfigurationReader src)
        where TPocoType : new()
    {
        return src.Get(typeof(TPocoType).Name, new Lazy<TPocoType>(() => new TPocoType()));
    }

    /// <summary>
    /// Saves a typed configuration value using the type name as the key.
    /// </summary>
    /// <typeparam name="TPocoType">The configuration value type.</typeparam>
    /// <param name="src">The configuration store.</param>
    /// <param name="value">The value to save.</param>
    public static void Set<TPocoType>(this IConfiguration src, TPocoType value)
        where TPocoType : new()
    {
        src.Set(typeof(TPocoType).Name, value);
    }

    /// <summary>
    /// Removes a typed configuration value using the type name as the key.
    /// </summary>
    /// <typeparam name="TPocoType">The configuration value type.</typeparam>
    /// <param name="src">The configuration store.</param>
    public static void Remove<TPocoType>(this IConfiguration src)
        where TPocoType : new()
    {
        src.Remove(typeof(TPocoType).Name);
    }
}
