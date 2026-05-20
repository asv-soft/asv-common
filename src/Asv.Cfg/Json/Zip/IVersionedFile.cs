using Asv.Common;

namespace Asv.Cfg;

/// <summary>
/// Represents a configuration file that exposes its semantic version.
/// </summary>
public interface IVersionedFile : IConfiguration
{
    /// <summary>
    /// Gets the configuration file version.
    /// </summary>
    SemVersion FileVersion { get; }
}
