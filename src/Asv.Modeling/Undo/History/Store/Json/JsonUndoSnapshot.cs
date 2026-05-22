using System.Text.Json.Serialization;

namespace Asv.Modeling;

/// <summary>
/// JSON-serializable representation of an undo snapshot.
/// </summary>
public class JsonUndoSnapshot
{
    /// <summary>
    /// Gets or sets the serialized navigation path to the change target.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Gets or sets the change registration identifier.
    /// </summary>
    public string ChangeId { get; set; }

    /// <summary>
    /// Gets or sets the serialized identifier of the change payload.
    /// </summary>
    public string DataRefId { get; set; }

    /// <summary>
    /// Gets or sets the base64-encoded in-memory change payload.
    /// </summary>
    public string Base64 { get; set; }
}

[JsonSourceGenerationOptions(
    WriteIndented = false,
    GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata
)]
[JsonSerializable(typeof(JsonUndoSnapshot))]
internal partial class JsonUndoSnapshotJsonContext : JsonSerializerContext;
