using System.Text;
using Newtonsoft.Json;

namespace Asv.Store;

public static class JsonPackageSettings
{
    // Default serializer settings for Newtonsoft.Json (cached as static readonly)
    public static readonly JsonSerializerSettings SerializerSettings = new()
    {
        Formatting = Formatting.Indented, // Pretty-print JSON with indentation
        NullValueHandling = NullValueHandling.Ignore, // Skip null values during serialization
        DefaultValueHandling = DefaultValueHandling.Populate, // Skip default values
        TypeNameHandling = TypeNameHandling.Auto, // Enable polymorphic serialization when needed

        // ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // Uncomment if circular references are possible
        // ContractResolver = new CamelCasePropertyNamesContractResolver() // Uncomment for camelCase naming
    };

    // Reusable JsonSerializer instance (thread-safe in Newtonsoft.Json)
    public static readonly JsonSerializer Serializer = JsonSerializer.Create(SerializerSettings);

    // Default encoding: UTF-8 without BOM (standard for JSON)
    public static readonly Encoding DefaultEncoding = new UTF8Encoding(
        encoderShouldEmitUTF8Identifier: false
    );
}
