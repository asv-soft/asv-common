using System;

namespace Asv.Cfg;

/// <summary>
/// Represents errors that occur while reading or writing configuration data.
/// </summary>
public class ConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    public ConfigurationException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ConfigurationException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="inner">The inner exception that caused this error.</param>
    public ConfigurationException(string message, Exception inner)
        : base(message, inner) { }
}
