using System;
using System.Collections.Generic;
using R3;

namespace Asv.Cfg
{
    /// <summary>
    /// Provides read and write access to a configuration store.
    /// </summary>
    public interface IConfiguration : IConfigurationReader, IDisposable
    {
        /// <summary>
        /// Saves a value by key.
        /// </summary>
        /// <typeparam name="TPocoType">The configuration value type.</typeparam>
        /// <param name="key">The configuration key.</param>
        /// <param name="value">The value to save.</param>
        void Set<TPocoType>(string key, TPocoType value);

        /// <summary>
        /// Removes a value by key.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        void Remove(string key);

        /// <summary>
        /// Gets an observable sequence of configuration errors.
        /// </summary>
        Observable<ConfigurationException> OnError { get; }

        /// <summary>
        /// Gets an observable sequence of changed configuration values.
        /// </summary>
        Observable<KeyValuePair<string, object?>> OnChanged { get; }
    }
}
