using System.IO.Packaging;
using System.Runtime.Serialization;
using System.Xml;
using Asv.IO;

namespace Asv.Store;

/// <summary>
/// Represents a package part that stores a single (scalar) metadata object of type <typeparamref name="TMetadata"/>
/// serialized as XML using <see cref="DataContractSerializer"/>.
/// </summary>
/// <typeparam name="TMetadata">The type of the metadata object. Must be marked with <see cref="DataContractAttribute"/>
/// (or be serializable via DataContract rules).</typeparam>
public class XmlMetadataAsvPackagePart<TMetadata>(
    Uri path,
    AsvPackageContext context,
    AsvPackagePart? parent = null,
    string contentType = "application/xml",
    CompressionOption compression = CompressionOption.Maximum
) : MetadataAsvPackagePart<TMetadata>(path, context, parent, contentType, compression)
{
    /// <summary>
    /// Cached DataContractSerializer instance for type <typeparamref name="TMetadata"/>.
    /// DataContractSerializer is thread-safe after construction, so a single static instance can be shared.
    /// </summary>
    private static readonly DataContractSerializer Serializer = new(typeof(TMetadata));

    /// <summary>
    /// Returns an empty collection because this part stores only a single scalar value and has no child parts.
    /// </summary>
    /// <returns>An empty enumerable of <see cref="AsvPackagePart"/>.</returns>
    public override IEnumerable<AsvPackagePart> GetChildren()
    {
        return Array.Empty<AsvPackagePart>();
    }

    /// <summary>
    /// Serializes the metadata object to XML and writes it to the provided stream.
    /// The XML is formatted with indentation for better readability.
    /// </summary>
    /// <param name="stream">The stream to write the XML data to.</param>
    /// <param name="metadata">The metadata object to serialize.</param>
    protected override void InternalWrite(Stream stream, TMetadata metadata)
    {
        // Create an XmlWriter with indentation enabled for human-readable output
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true });
        Serializer.WriteObject(writer, metadata);

        // Flush is called automatically when the XmlWriter is disposed
    }

    /// <summary>
    /// Deserializes an XML stream into a metadata object of type <typeparamref name="TMetadata"/>.
    /// </summary>
    /// <param name="stream">The stream containing the XML data to read.</param>
    /// <returns>The deserialized metadata object, or null if deserialization fails (as per DataContractSerializer behavior).</returns>
    protected override TMetadata? InternalRead(Stream stream)
    {
        // Create an XmlReader with default settings (no external entity resolution for security)
        using var reader = XmlReader.Create(stream);
        return (TMetadata?)Serializer.ReadObject(reader);
    }
}
