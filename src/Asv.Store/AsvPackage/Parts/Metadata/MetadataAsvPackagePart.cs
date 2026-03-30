using System.IO.Packaging;

namespace Asv.Store;

/// <summary>
/// Abstract base class for a package part that stores a single (scalar) metadata object of type <typeparamref name="TMetadata"/>.
/// The actual serialization format is defined by derived classes (e.g., JSON, MessagePack, etc.).
/// </summary>
/// <typeparam name="TMetadata">The type of the metadata object stored in this part.</typeparam>
public abstract class MetadataAsvPackagePart<TMetadata>(
    Uri path,
    AsvPackageContext context,
    AsvPackagePart? parent,
    string contentType,
    CompressionOption compressionOption
) : AsvPackagePart(context, parent), IMetadataPart<TMetadata>
{
    /// <summary>
    /// Writes the metadata object to the package part.
    /// If the part already exists with a different content type, an exception is thrown.
    /// If the metadata is null, the existing part (if any) is deleted.
    /// </summary>
    /// <param name="metadata">The metadata object to write. Can be null to remove the part.</param>
    public void Write(TMetadata? metadata)
    {
        EnsureWriteAccess();

        using (Context.Lock.EnterScope())
        {
            // If the part already exists, verify its content type matches the expected one
            if (Context.Package.PartExists(path))
            {
                var existContentType = Context.Package.GetPart(path).ContentType;
                if (existContentType != contentType)
                {
                    throw new InvalidOperationException(
                        $"Want to update part {path}, but it exists and has a different content type '{existContentType}'. Expected '{contentType}'."
                    );
                }

                // Remove the existing part so it can be recreated
                Context.Package.DeletePart(path);
            }

            // If metadata is null, we just removed the part (if it existed) and are done
            if (metadata == null)
            {
                return;
            }

            // Create the new part with the specified content type and compression
            var part = Context.Package.CreatePart(path, contentType, compressionOption);

            // Open the stream and delegate the actual serialization to the derived class
            using var stream = part.GetStream(FileMode.Create, FileAccess.ReadWrite);
            InternalWrite(stream, metadata);
        }
    }

    /// <summary>
    /// Writes the metadata object to the provided stream.
    /// Implemented by derived classes to perform format-specific serialization.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="metadata">The metadata object to serialize.</param>
    protected abstract void InternalWrite(Stream stream, TMetadata metadata);

    /// <summary>
    /// Reads the metadata object from the package part.
    /// Returns default(TMetadata) if the part does not exist.
    /// Throws if the existing part has an unexpected content type.
    /// </summary>
    /// <returns>The deserialized metadata object, or default if the part is missing.</returns>
    public TMetadata? Read()
    {
        EnsureReadAccess();

        using (Context.Lock.EnterScope())
        {
            if (Context.Package.PartExists(path))
            {
                var existContentType = Context.Package.GetPart(path).ContentType;
                if (existContentType != contentType)
                {
                    throw new InvalidOperationException(
                        $"Want to read part {path}, but it has a different content type '{existContentType}'. Expected '{contentType}'."
                    );
                }

                var part = Context.Package.GetPart(path);
                using var stream = part.GetStream(FileMode.Open, FileAccess.Read);
                return InternalRead(stream);
            }

            // Part does not exist – return default value
            return default;
        }
    }

    /// <summary>
    /// Reads and deserializes the metadata object from the provided stream.
    /// Implemented by derived classes to perform format-specific deserialization.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The deserialized metadata object.</returns>
    protected abstract TMetadata? InternalRead(Stream stream);

    /// <summary>
    /// No flushing is required for this type of part, as all data is written immediately.
    /// </summary>
    public override void InternalFlush()
    {
        // Nothing to flush
    }
}
