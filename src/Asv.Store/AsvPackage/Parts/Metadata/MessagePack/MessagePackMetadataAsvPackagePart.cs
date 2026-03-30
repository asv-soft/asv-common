using System.IO.Packaging;
using MessagePack;

namespace Asv.Store;

/// <summary>
/// Represents a package part that stores a single metadata object of type <typeparamref name="TMetadata"/>
/// serialized using the MessagePack binary format.
/// </summary>
/// <typeparam name="TMetadata">The type of the metadata object.
/// It should be annotated with MessagePack attributes (e.g., <see cref="MessagePackObjectAttribute"/> and <see cref="KeyAttribute"/>)
/// for optimal serialization.</typeparam>
public class MessagePackMetadataAsvPackagePart<TMetadata>(
    Uri path,
    AsvPackageContext context,
    AsvPackagePart? parent = null,
    string contentType = "application/msgpack",
    CompressionOption compression = CompressionOption.Maximum
) : MetadataAsvPackagePart<TMetadata>(path, context, parent, contentType, compression)
{
    /// <summary>
    /// Returns an empty collection because this part contains only a single scalar metadata value
    /// and does not have any child parts.
    /// </summary>
    /// <returns>An empty enumerable of <see cref="AsvPackagePart"/>.</returns>
    public override IEnumerable<AsvPackagePart> GetChildren()
    {
        return Array.Empty<AsvPackagePart>();
    }

    /// <summary>
    /// Serializes the metadata object to the provided stream in MessagePack format.
    /// The serialization is performed directly to the stream with zero allocation where possible,
    /// providing excellent performance and minimal memory usage.
    /// </summary>
    /// <param name="stream">The stream to which the serialized data will be written.</param>
    /// <param name="metadata">The metadata object to serialize.</param>
    protected override void InternalWrite(Stream stream, TMetadata metadata)
    {
        // MessagePackSerializer is static, thread-safe and highly optimized
        MessagePackSerializer.Serialize(stream, metadata);
    }

    /// <summary>
    /// Deserializes a MessagePack-formatted stream back into a metadata object of type <typeparamref name="TMetadata"/>.
    /// </summary>
    /// <param name="stream">The stream containing the MessagePack binary data.</param>
    /// <returns>The deserialized metadata object. Returns default(<typeparamref name="TMetadata"/>)
    /// if the stream is empty or deserialization fails gracefully.</returns>
    protected override TMetadata? InternalRead(Stream stream)
    {
        return MessagePackSerializer.Deserialize<TMetadata>(stream);
    }
}
