using System.IO.Packaging;
using System.Text;
using Newtonsoft.Json;

namespace Asv.Store;

/// <summary>
/// Represents a package part that stores a single metadata object of type <typeparamref name="TMetadata"/> as JSON.
/// </summary>
/// <typeparam name="TMetadata">The type of the metadata object to serialize/deserialize.</typeparam>
public class JsonMetadataAsvPackagePart<TMetadata>(
    Uri path,
    AsvPackageContext context,
    AsvPackagePart? parent = null,
    string contentType = "application/json",
    CompressionOption compression = CompressionOption.Maximum,
    Encoding? encoding = null
) : MetadataAsvPackagePart<TMetadata>(path, context, parent, contentType, compression)
{
    /// <summary>
    /// Returns an empty collection since this part stores a single scalar value and has no children.
    /// </summary>
    public override IEnumerable<AsvPackagePart> GetChildren() => Array.Empty<AsvPackagePart>();

    /// <summary>
    /// Writes the metadata object to the specified stream as JSON.
    /// </summary>
    /// <param name="stream">The target stream to write to.</param>
    /// <param name="metadata">The metadata object to serialize.</param>
    protected override void InternalWrite(Stream stream, TMetadata metadata)
    {
        using var streamWriter = new StreamWriter(
            stream,
            encoding ?? JsonPackageSettings.DefaultEncoding,
            leaveOpen: true
        );
        using var jsonWriter = new JsonTextWriter(streamWriter);
        jsonWriter.Formatting = JsonPackageSettings.SerializerSettings.Formatting;

        JsonPackageSettings.Serializer.Serialize(jsonWriter, metadata, typeof(TMetadata));
        jsonWriter.Flush(); // Ensure all data is written to the stream
    }

    /// <summary>
    /// Reads and deserializes the JSON data from the specified stream into a metadata object.
    /// </summary>
    /// <param name="stream">The source stream to read from.</param>
    /// <returns>The deserialized metadata object, or default if the stream is empty.</returns>
    protected override TMetadata? InternalRead(Stream stream)
    {
        // Return default if stream is empty
        if (stream.Length == 0)
        {
            return default;
        }

        using var streamReader = new StreamReader(
            stream,
            encoding ?? JsonPackageSettings.DefaultEncoding,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true
        );
        using var jsonReader = new JsonTextReader(streamReader);

        return JsonPackageSettings.Serializer.Deserialize<TMetadata>(jsonReader);
    }
}
