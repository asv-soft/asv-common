using System.Text.Json.Serialization;

namespace Asv.Modeling;

public class UndoSnapshot : IUndoSnapshot
{
    public required NavPath Path { get; set; }
    public required string ChangeId { get; set; }
    public required Ulid DataRefId { get; set; }
    public byte[]? Data { get; set; }
}

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
