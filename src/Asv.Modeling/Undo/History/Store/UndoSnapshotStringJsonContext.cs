using System.Text.Json.Serialization;

namespace Asv.Modeling;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata
)]
[JsonSerializable(typeof(UndoSnapshot<string>))]
internal partial class UndoSnapshotStringJsonContext : JsonSerializerContext;
