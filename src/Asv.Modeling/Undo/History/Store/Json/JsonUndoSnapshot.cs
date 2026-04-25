using System.Text.Json.Serialization;

namespace Asv.Modeling;



public class JsonUndoSnapshot
{
    public string Path { get; set; }
    public string ChangeId { get; set; }
    public string DataRefId { get; set; }
    public string Base64 { get; set; }
}

[JsonSourceGenerationOptions(
    WriteIndented = false,
    GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata
)]
[JsonSerializable(typeof(JsonUndoSnapshot))]
internal partial class JsonUndoSnapshotJsonContext : JsonSerializerContext;
