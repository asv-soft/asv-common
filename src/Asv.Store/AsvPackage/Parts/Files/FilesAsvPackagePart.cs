#nullable enable

using System.Collections.ObjectModel;
using System.IO.Packaging;
using Newtonsoft.Json;

namespace Asv.Store;

public class FilesAsvPackagePart(
    Uri rootUri,
    AsvPackageContext context,
    AsvPackagePart? parent = null,
    string manifestContentType = "application/vnd.asv.store.files.manifest+json",
    CompressionOption compression = CompressionOption.Maximum
) : AsvPackagePart(context, parent), IFileStorePart
{
    private const int ManifestVersion = 1;
    private const string ManifestFileName = "manifest.json";
    private const string ContentDirectoryName = "content";
    private readonly Uri _manifestUri = CreatePartUri(
        $"{NormalizeRootPath(rootUri)}{ManifestFileName}"
    );
    private readonly string _contentRootPath = $"{NormalizeRootPath(rootUri)}{ContentDirectoryName}/";

    public IEnumerable<IStoredFile> Enumerate(
        string? relativeDirectory = null,
        bool recursive = true
    )
    {
        EnsureReadAccess();
        var directory = NormalizeDirectoryPath(relativeDirectory);

        using (Context.Lock.EnterScope())
        {
            var manifest = LoadManifest();
            return manifest
                .Files.Where(x => IsInDirectory(x.Key, directory, recursive))
                .Where(x => Context.Package.PartExists(CreateContentUri(x.Key)))
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => CreateItem(x.Key, x.Value))
                .ToArray();
        }
    }

    public IStoredFile? Get(string relativePath)
    {
        EnsureReadAccess();
        var normalizedPath = NormalizeRelativePath(relativePath);

        using (Context.Lock.EnterScope())
        {
            var manifest = LoadManifest();
            if (manifest.Files.TryGetValue(normalizedPath, out var entry) == false)
            {
                return null;
            }

            return Context.Package.PartExists(CreateContentUri(normalizedPath))
                ? CreateItem(normalizedPath, entry)
                : null;
        }
    }

    public bool Exists(string relativePath)
    {
        EnsureReadAccess();
        var normalizedPath = NormalizeRelativePath(relativePath);

        using (Context.Lock.EnterScope())
        {
            var manifest = LoadManifest();
            return manifest.Files.ContainsKey(normalizedPath)
                && Context.Package.PartExists(CreateContentUri(normalizedPath));
        }
    }

    public void Write(
        string relativePath,
        Stream content,
        string contentType = "application/octet-stream",
        IReadOnlyDictionary<string, string>? metadata = null
    )
    {
        EnsureWriteAccess();
        ArgumentNullException.ThrowIfNull(content);
        var normalizedPath = NormalizeRelativePath(relativePath);
        var normalizedContentType = NormalizeContentType(contentType);
        var metadataSnapshot = CreateMetadataSnapshot(metadata);
        var contentUri = CreateContentUri(normalizedPath);

        using (Context.Lock.EnterScope())
        {
            var manifest = LoadManifest();
            if (Context.Package.PartExists(contentUri))
            {
                Context.Package.DeletePart(contentUri);
            }

            var part = Context.Package.CreatePart(contentUri, normalizedContentType, compression);
            using (var packageStream = part.GetStream(FileMode.Create, FileAccess.ReadWrite))
            {
                content.CopyTo(packageStream);
            }

            manifest.Files[normalizedPath] = new FileManifestEntry
            {
                ContentType = normalizedContentType,
                Metadata = metadataSnapshot,
            };
            SaveManifest(manifest);
        }
    }

    public bool Delete(string relativePath)
    {
        EnsureWriteAccess();
        var normalizedPath = NormalizeRelativePath(relativePath);
        var contentUri = CreateContentUri(normalizedPath);

        using (Context.Lock.EnterScope())
        {
            var manifest = LoadManifest();
            var removedFromManifest = manifest.Files.Remove(normalizedPath);
            var removedContent = Context.Package.PartExists(contentUri);
            if (removedContent)
            {
                Context.Package.DeletePart(contentUri);
            }

            if (removedFromManifest)
            {
                SaveManifest(manifest);
            }

            return removedFromManifest || removedContent;
        }
    }

    public override IEnumerable<AsvPackagePart> GetChildren()
    {
        return [];
    }

    public override void InternalFlush()
    {
        Context.Package.Flush();
    }

    private FileManifest LoadManifest()
    {
        if (Context.Package.PartExists(_manifestUri) == false)
        {
            return new FileManifest();
        }

        var part = Context.Package.GetPart(_manifestUri);
        if (part.ContentType != manifestContentType)
        {
            throw new InvalidOperationException(
                $"Files manifest part {_manifestUri} has content type '{part.ContentType}', but '{manifestContentType}' was expected."
            );
        }

        using var stream = part.GetStream(FileMode.Open, FileAccess.Read);
        if (stream.Length == 0)
        {
            return new FileManifest();
        }

        using var reader = new StreamReader(stream);
        using var jsonReader = new JsonTextReader(reader);
        var serializer = JsonSerializer.CreateDefault();
        var manifest = serializer.Deserialize<FileManifest>(jsonReader) ?? new FileManifest();
        if (manifest.Version != ManifestVersion)
        {
            throw new InvalidOperationException(
                $"Files manifest part {_manifestUri} has version '{manifest.Version}', but '{ManifestVersion}' was expected."
            );
        }

        manifest.Files ??= new Dictionary<string, FileManifestEntry>(StringComparer.Ordinal);
        foreach (var item in manifest.Files.Values)
        {
            item.Metadata ??= new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return manifest;
    }

    private void SaveManifest(FileManifest manifest)
    {
        if (Context.Package.PartExists(_manifestUri))
        {
            var part = Context.Package.GetPart(_manifestUri);
            if (part.ContentType != manifestContentType)
            {
                throw new InvalidOperationException(
                    $"Files manifest part {_manifestUri} has content type '{part.ContentType}', but '{manifestContentType}' was expected."
                );
            }

            Context.Package.DeletePart(_manifestUri);
        }

        if (manifest.Files.Count == 0)
        {
            return;
        }

        var manifestPart = Context.Package.CreatePart(_manifestUri, manifestContentType, compression);
        using var stream = manifestPart.GetStream(FileMode.Create, FileAccess.ReadWrite);
        using var writer = new StreamWriter(stream);
        using var jsonWriter = new JsonTextWriter(writer)
        {
            Formatting = Formatting.Indented,
            CloseOutput = false,
        };
        var serializer = JsonSerializer.CreateDefault();
        serializer.Serialize(jsonWriter, manifest);
        jsonWriter.Flush();
        writer.Flush();
    }

    private IStoredFile CreateItem(string relativePath, FileManifestEntry entry)
    {
        var contentUri = CreateContentUri(relativePath);
        var length = GetPartLength(contentUri);
        return new FilePartItem(
            Context,
            contentUri,
            relativePath,
            Path.GetFileName(relativePath),
            entry.ContentType,
            entry.Metadata,
            length
        );
    }

    private long GetPartLength(Uri contentUri)
    {
        if (Context.Package.PartExists(contentUri) == false)
        {
            return 0;
        }

        var part = Context.Package.GetPart(contentUri);
        using var stream = part.GetStream(FileMode.Open, FileAccess.Read);
        return stream.Length;
    }

    private Uri CreateContentUri(string relativePath)
    {
        return CreatePartUri($"{_contentRootPath}{EscapeRelativePath(relativePath)}");
    }

    private static bool IsInDirectory(string relativePath, string directory, bool recursive)
    {
        if (directory.Length == 0)
        {
            return recursive || relativePath.Contains('/') == false;
        }

        var prefix = $"{directory}/";
        if (relativePath.StartsWith(prefix, StringComparison.Ordinal) == false)
        {
            return false;
        }

        return recursive || relativePath[prefix.Length..].Contains('/') == false;
    }

    private static string NormalizeRootPath(Uri rootUri)
    {
        ArgumentNullException.ThrowIfNull(rootUri);
        if (rootUri.IsAbsoluteUri)
        {
            throw new ArgumentException("Files root URI must be relative.", nameof(rootUri));
        }

        var root = rootUri.OriginalString.Replace('\\', '/').Trim();
        if (string.IsNullOrEmpty(root))
        {
            root = "/";
        }

        if (root.Contains('?') || root.Contains('#'))
        {
            throw new ArgumentException(
                "Files root URI must not contain query or fragment.",
                nameof(rootUri)
            );
        }

        if (root.StartsWith('/') == false)
        {
            root = $"/{root}";
        }

        if (root.EndsWith('/') == false)
        {
            root = $"{root}/";
        }

        return root;
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        var path = relativePath.Replace('\\', '/').Trim();
        if (path.StartsWith('/'))
        {
            throw new ArgumentException(
                "File path must be relative and must not start with '/'.",
                nameof(relativePath)
            );
        }

        if (path.EndsWith('/'))
        {
            throw new ArgumentException("File path must include a file name.", nameof(relativePath));
        }

        var segments = path.Split('/');
        ValidatePathSegments(segments, nameof(relativePath));
        return string.Join("/", segments);
    }

    private static string NormalizeDirectoryPath(string? relativeDirectory)
    {
        if (string.IsNullOrWhiteSpace(relativeDirectory))
        {
            return string.Empty;
        }

        var path = relativeDirectory.Replace('\\', '/').Trim().Trim('/');
        if (path.Length == 0)
        {
            return string.Empty;
        }

        var segments = path.Split('/');
        ValidatePathSegments(segments, nameof(relativeDirectory));
        return string.Join("/", segments);
    }

    private static void ValidatePathSegments(string[] segments, string parameterName)
    {
        foreach (var segment in segments)
        {
            if (
                string.IsNullOrWhiteSpace(segment)
                || string.Equals(segment, ".", StringComparison.Ordinal)
                || string.Equals(segment, "..", StringComparison.Ordinal)
            )
            {
                throw new ArgumentException(
                    "File path must not contain empty, current, or parent directory segments.",
                    parameterName
                );
            }
        }
    }

    private static string EscapeRelativePath(string relativePath)
    {
        return string.Join("/", relativePath.Split('/').Select(Uri.EscapeDataString));
    }

    private static Uri CreatePartUri(string path)
    {
        return new Uri(path, UriKind.Relative);
    }

    private static string NormalizeContentType(string contentType)
    {
        return string.IsNullOrWhiteSpace(contentType)
            ? "application/octet-stream"
            : contentType.Trim();
    }

    private static Dictionary<string, string> CreateMetadataSnapshot(
        IReadOnlyDictionary<string, string>? metadata
    )
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (metadata is null)
        {
            return result;
        }

        foreach (var pair in metadata)
        {
            if (pair.Key is null)
            {
                throw new ArgumentException("Metadata key must not be null.", nameof(metadata));
            }

            if (pair.Value is null)
            {
                throw new ArgumentException("Metadata value must not be null.", nameof(metadata));
            }

            result.Add(pair.Key, pair.Value);
        }

        return result;
    }

    private sealed class FilePartItem(
        AsvPackageContext context,
        Uri contentUri,
        string relativePath,
        string name,
        string contentType,
        IReadOnlyDictionary<string, string> metadata,
        long length
    ) : IStoredFile
    {
        private readonly IReadOnlyDictionary<string, string> _metadata =
            new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(metadata, StringComparer.Ordinal)
            );

        public string RelativePath { get; } = relativePath;
        public string Name { get; } = name;
        public string ContentType { get; } = contentType;
        public IReadOnlyDictionary<string, string> Metadata => _metadata;
        public long Length { get; } = length;

        public Stream OpenRead()
        {
            if (
                context.Package.FileOpenAccess != FileAccess.Read
                && context.Package.FileOpenAccess != FileAccess.ReadWrite
            )
            {
                throw new InvalidOperationException("Package is not opened with read access");
            }

            using (context.Lock.EnterScope())
            {
                if (context.Package.PartExists(contentUri) == false)
                {
                    throw new FileNotFoundException(
                        $"Package file '{RelativePath}' does not exist.",
                        RelativePath
                    );
                }

                var part = context.Package.GetPart(contentUri);
                return part.GetStream(FileMode.Open, FileAccess.Read);
            }
        }
    }

    private sealed class FileManifest
    {
        public int Version { get; set; } = ManifestVersion;
        public Dictionary<string, FileManifestEntry> Files { get; set; } =
            new(StringComparer.Ordinal);
    }

    private sealed class FileManifestEntry
    {
        public string ContentType { get; set; } = "application/octet-stream";
        public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.Ordinal);
    }
}
