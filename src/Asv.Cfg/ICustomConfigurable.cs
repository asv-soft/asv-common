namespace Asv.Cfg;

/// <summary>
/// If you want to save/load configuration from/to IConfiguration by custom way, you can implement this interface
/// IConfiguration will call Load/Save methods when it needs to save/load configuration
/// </summary>
public interface ICustomConfigurable
{
    /// <summary>
    /// Loads custom configuration data from the specified configuration store.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="configuration">The configuration store.</param>
    public void Load(string key, IConfiguration configuration);

    /// <summary>
    /// Saves custom configuration data to the specified configuration store.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="configuration">The configuration store.</param>
    public void Save(string key, IConfiguration configuration);
}
