using System;
using System.Collections.Generic;

namespace Asv.Cfg;

/// <summary>
/// Provides read-only access to a configuration store.
/// </summary>
public interface IConfigurationReader
{
    /// <summary>
    /// Gets keys reserved by the configuration store.
    /// </summary>
    IEnumerable<string> ReservedParts { get; }

    /// <summary>
    /// Gets keys currently available in the configuration store.
    /// </summary>
    IEnumerable<string> AvailableParts { get; }

    /// <summary>
    /// Determines whether the specified key exists.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns><see langword="true"/> if the key exists; otherwise, <see langword="false"/>.</returns>
    bool Exist(string key);

    /// <summary>
    /// Gets a value by key or stores and returns a default value when the key is missing.
    /// </summary>
    /// <typeparam name="TPocoType">The configuration value type.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value factory used when the key is missing.</param>
    /// <returns>The configuration value.</returns>
    TPocoType Get<TPocoType>(string key, Lazy<TPocoType> defaultValue);
}
