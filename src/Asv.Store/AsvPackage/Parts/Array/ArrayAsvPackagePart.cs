using System.IO.Packaging;

namespace Asv.Store;

public abstract class ArrayAsvPackagePart<TRow>(
    Uri path,
    AsvPackageContext context,
    AsvPackagePart? parent,
    string contentType,
    CompressionOption compressionOption
) : AsvPackagePart(context, parent), IArrayPart<TRow>
{
    public override IEnumerable<AsvPackagePart> GetChildren()
    {
        return [];
    }

    public override void InternalFlush()
    {
        // do nothing
    }

    public async ValueTask Read(Action<TRow> visitor, CancellationToken cancel)
    {
        EnsureReadAccess();

        Stream? stream;
        using (Context.Lock.EnterScope())
        {
            stream = null;
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
                stream = part.GetStream(FileMode.Open, FileAccess.Read);
            }
        }

        if (stream == null)
        {
            return;
        }

        await using (stream)
        {
            await InternalRead(stream, visitor, cancel);
        }
    }

    protected abstract ValueTask InternalRead(
        Stream stream,
        Action<TRow> visitor,
        CancellationToken cancel
    );

    public async ValueTask Write(IEnumerable<TRow> values, CancellationToken cancel)
    {
        EnsureWriteAccess();

        Stream stream;
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

            // Create the new part with the specified content type and compression
            var part = Context.Package.CreatePart(path, contentType, compressionOption);

            // Open the stream and delegate the actual serialization to the derived class
            stream = part.GetStream(FileMode.Create, FileAccess.ReadWrite);
        }

        await using (stream)
        {
            await InternalWrite(stream, values, cancel);
        }
    }

    protected abstract ValueTask InternalWrite(
        Stream stream,
        IEnumerable<TRow> values,
        CancellationToken cancel
    );
}
