using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Asv.Cfg;

public static class JsonHelper
{
    public static JsonSerializer CreateDefaultJsonSerializer()
    {
        var serializer = new JsonSerializer
        {
            Formatting = Formatting.Indented,
        };
        serializer.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy(),true));
        return serializer;
    }
}