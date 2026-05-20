using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Asv.Cfg;

/// <summary>
/// Provides JSON serializer helpers for configuration stores.
/// </summary>
public static class JsonHelper
{
    /// <summary>
    /// Creates the default JSON serializer used by configuration stores.
    /// </summary>
    /// <returns>The configured JSON serializer.</returns>
    public static JsonSerializer CreateDefaultJsonSerializer()
    {
        var serializer = new JsonSerializer { Formatting = Formatting.Indented };
        serializer.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy(), true));
        return serializer;
    }
}
