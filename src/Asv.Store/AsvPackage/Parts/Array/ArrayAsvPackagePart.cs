using System.IO.Packaging;
using Asv.IO;
using DotNext.Threading.Tasks;

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

        Context.Lock.Enter();
        Stream? stream = null;
        try
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
                stream = part.GetStream(FileMode.Open, FileAccess.Read);
                InternalRead(stream, visitor, cancel).Wait();
            }
        }
        finally
        {
            stream?.Dispose();
            Context.Lock.Exit();
        }
    }

    protected abstract ValueTask InternalRead(
        Stream stream,
        Action<TRow> visitor,
        CancellationToken cancel
    );

    public ValueTask Write(IEnumerable<TRow> values, CancellationToken cancel)
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

            // Create the new part with the specified content type and compression
            var part = Context.Package.CreatePart(path, contentType, compressionOption);

            // Open the stream and delegate the actual serialization to the derived class
            using var stream = part.GetStream(FileMode.Create, FileAccess.ReadWrite);
            return InternalWrite(stream, values, cancel);
        }
    }

    protected abstract ValueTask InternalWrite(
        Stream stream,
        IEnumerable<TRow> values,
        CancellationToken cancel
    );
}
