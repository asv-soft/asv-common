#nullable enable

using System.IO.Packaging;

namespace Asv.Store;

public abstract class DictionaryAsvPackage(
    Uri uriPart,
    string contentType,
    CompressionOption compression,
    AsvPackageContext context,
    AsvPackagePart? parent = null
) : AsvPackagePart(context, parent), IDictionaryPart
{
    protected Uri UriPart { get; } = uriPart;
    protected string ContentType { get; } = contentType;
    protected CompressionOption Compression { get; } = compression;

    public abstract TDto? Read<TDto>(params string[] path);

    public abstract TDto? Read<TDto>(IEnumerable<string> path);
    public abstract void Write<TDto>(TDto? value, params string[] path);
    public abstract void Write<TDto>(TDto? value, IEnumerable<string> path);

    public override IEnumerable<AsvPackagePart> GetChildren()
    {
        return [];
    }

    protected void EnsurePartContentType()
    {
        if (!Context.Package.PartExists(UriPart))
        {
            return;
        }

        var existContentType = Context.Package.GetPart(UriPart).ContentType;
        if (existContentType != ContentType)
        {
            throw new InvalidOperationException(
                $"Package part {UriPart} has content type '{existContentType}', but '{ContentType}' was expected."
            );
        }
    }
}
