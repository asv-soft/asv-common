namespace Asv.Cfg;

/// <summary>
/// If you want to save/load configuration from/to IConfiguration by custom way, you can implement this interface
/// IConfiguration will call Load/Save methods when it needs to save/load configuration
/// </summary>
public interface ICustomConfigurable
{
    public void Load(string key, IConfiguration configuration);
    public void Save(string key, IConfiguration configuration);
}
