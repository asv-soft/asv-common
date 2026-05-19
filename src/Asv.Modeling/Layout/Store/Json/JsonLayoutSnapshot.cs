using System.Text.Json.Serialization;

namespace Asv.Modeling;

public class JsonLayoutSnapshot
{
    public required string Path { get; set; }
    public required string LayoutId { get; set; }
    public required string Base64 { get; set; }
}

[JsonSerializable(typeof(JsonLayoutSnapshot))]
internal partial class JsonLayoutSnapshotJsonContext : JsonSerializerContext;
