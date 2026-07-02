#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.IO.Packaging;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace Asv.Store;

public class XmlDictionaryAsvPackage(
    Uri uriPart,
    AsvPackageContext context,
    AsvPackagePart? parent = null,
    string contentType = "application/xml",
    CompressionOption compression = CompressionOption.Maximum,
    string description = ""
) : DictionaryAsvPackage(uriPart, contentType, compression, context, parent)
{
    private const string RootElementName = "dictionary";
    private const string FormatAttributeName = "format";
    private const string VersionAttributeName = "version";
    private const string DescriptionAttributeName = "description";
    private const string FormatName = "Asv.Store.Dictionary";
    private const string FormatVersion = "1";
    private const string TypeAttributeName = "type";
    private const string AssemblyAttributeName = "assembly";
    private readonly string _description = description ?? string.Empty;
    private readonly XDocument _document = LoadDocument(context, uriPart, contentType);
    private bool _hasChanges;

    [return: MaybeNull]
    public override TDto Read<TDto>(params string[] path)
    {
        return Read<TDto>((IEnumerable<string>)path);
    }

    [return: MaybeNull]
    public override TDto Read<TDto>(IEnumerable<string> path)
    {
        EnsureReadAccess();
        var segments = NormalizePath(path);

        using (Context.Lock.EnterScope())
        {
            foreach (var leaf in FindLeafCandidates(segments))
            {
                if (MatchesType<TDto>(leaf) && TryReadValue<TDto>(leaf, out var value))
                {
                    return value;
                }
            }

            return default;
        }
    }

    public override void Write<TDto>(TDto? value, params string[] path)
        where TDto : default
    {
        Write(value, (IEnumerable<string>)path);
    }

    public override void Write<TDto>(TDto? value, IEnumerable<string> path)
        where TDto : default
    {
        EnsureWriteAccess();
        var segments = NormalizePath(path);

        using (Context.Lock.EnterScope())
        {
            if (value is null)
            {
                RemoveTypedCore<TDto>(segments);
                return;
            }

            var leaf = GetOrCreateLeaf<TDto>(segments);
            SetStoredType<TDto>(leaf);
            leaf.RemoveNodes();
            WriteValue(leaf, value);
            _hasChanges = true;
        }
    }

    public override void InternalFlush()
    {
        EnsureWriteAccess();

        using (Context.Lock.EnterScope())
        {
            if (!_hasChanges)
            {
                return;
            }

            if (Context.Package.PartExists(UriPart))
            {
                EnsurePartContentType();
                Context.Package.DeletePart(UriPart);
            }

            if (_document.Root is null)
            {
                _hasChanges = false;
                return;
            }

            var part = Context.Package.CreatePart(UriPart, ContentType, Compression);
            using var stream = part.GetStream(FileMode.Create, FileAccess.ReadWrite);
            using var writer = XmlWriter.Create(
                stream,
                new XmlWriterSettings { Indent = true, CloseOutput = false }
            );
            _document.Save(writer);
            _hasChanges = false;
        }
    }

    private static XDocument LoadDocument(
        AsvPackageContext context,
        Uri uriPart,
        string contentType
    )
    {
        if (
            context.Package.FileOpenAccess != FileAccess.Read
            && context.Package.FileOpenAccess != FileAccess.ReadWrite
        )
        {
            return new XDocument();
        }

        using (context.Lock.EnterScope())
        {
            if (!context.Package.PartExists(uriPart))
            {
                return new XDocument();
            }

            var packagePart = context.Package.GetPart(uriPart);
            if (packagePart.ContentType != contentType)
            {
                throw new InvalidOperationException(
                    $"Package part {uriPart} has content type '{packagePart.ContentType}', but '{contentType}' was expected."
                );
            }

            using var stream = packagePart.GetStream(FileMode.Open, FileAccess.Read);
            using var reader = XmlReader.Create(
                stream,
                new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    IgnoreWhitespace = true,
                }
            );
            var document = XDocument.Load(reader);
            EnsureDictionaryRoot(document, uriPart);
            return document;
        }
    }

    private static void EnsureDictionaryRoot(XDocument document, Uri uriPart)
    {
        if (document.Root is null)
        {
            return;
        }

        if (HasLocalName(document.Root, RootElementName) == false)
        {
            throw new InvalidOperationException(
                $"XML dictionary part {uriPart} root must be '{RootElementName}', but '{document.Root.Name.LocalName}' was found."
            );
        }

        if (
            string.Equals(
                document.Root.Attribute(FormatAttributeName)?.Value,
                FormatName,
                StringComparison.Ordinal
            ) == false
        )
        {
            throw new InvalidOperationException(
                $"XML dictionary part {uriPart} must have '{FormatAttributeName}' attribute equal to '{FormatName}'."
            );
        }

        if (
            string.Equals(
                document.Root.Attribute(VersionAttributeName)?.Value,
                FormatVersion,
                StringComparison.Ordinal
            ) == false
        )
        {
            throw new InvalidOperationException(
                $"XML dictionary part {uriPart} must have '{VersionAttributeName}' attribute equal to '{FormatVersion}'."
            );
        }
    }

    private static string[] NormalizePath(IEnumerable<string> path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var segments = path.Select(x => x?.Trim())
            .Where(x => string.IsNullOrEmpty(x) == false)
            .Select(x => x!)
            .ToArray();

        if (segments.Length == 0)
        {
            throw new ArgumentException(
                "Dictionary path must contain at least one XML element name.",
                nameof(path)
            );
        }

        foreach (var segment in segments)
        {
            try
            {
                XmlConvert.VerifyName(segment);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException(
                    $"Dictionary path segment '{segment}' is not a valid XML element name.",
                    nameof(path),
                    ex
                );
            }
        }

        return segments;
    }

    private IEnumerable<XElement> FindLeafCandidates(string[] segments)
    {
        var dictionaryRoot = _document.Root;
        if (dictionaryRoot is null)
        {
            return [];
        }

        var current = dictionaryRoot
            .Elements()
            .Where(x => HasLocalName(x, segments[0]))
            .Where(x => segments.Length == 1 || IsTypedElement(x) == false)
            .ToArray();

        if (segments.Length == 1)
        {
            return current;
        }

        for (var i = 1; i < segments.Length; i++)
        {
            var isLeaf = i == segments.Length - 1;
            current = current
                .SelectMany(x => x.Elements())
                .Where(x => HasLocalName(x, segments[i]) && (isLeaf || IsTypedElement(x) == false))
                .ToArray();

            if (current.Length == 0)
            {
                return [];
            }
        }

        return current;
    }

    private XElement GetOrCreateLeaf<TDto>(string[] segments)
    {
        if (segments.Length == 1)
        {
            var root = GetOrCreateRoot(segments[0]);
            if (IsContainerElement(root))
            {
                throw new InvalidOperationException(
                    $"Dictionary path '{segments[0]}' is already used as a container."
                );
            }

            return root;
        }

        var parent = GetOrCreateRoot(segments[0]);
        if (IsTypedElement(parent))
        {
            throw new InvalidOperationException(
                $"Dictionary path '{segments[0]}' is already used as a value."
            );
        }

        for (var i = 1; i < segments.Length - 1; i++)
        {
            var segment = segments[i];
            var sameName = parent.Elements().Where(x => HasLocalName(x, segment)).ToArray();
            var next = sameName.FirstOrDefault(x => IsTypedElement(x) == false);

            if (next is null)
            {
                if (sameName.Length > 0)
                {
                    throw new InvalidOperationException(
                        $"Dictionary path segment '{segment}' is already used as a value."
                    );
                }

                next = new XElement(segment);
                parent.Add(next);
            }

            parent = next;
        }

        var leafName = segments[^1];
        var leaf = parent.Elements().FirstOrDefault(x => HasLocalName(x, leafName));

        if (leaf is not null)
        {
            if (IsContainerElement(leaf))
            {
                throw new InvalidOperationException(
                    $"Dictionary path '{string.Join("/", segments)}' is already used as a container."
                );
            }

            return leaf;
        }

        leaf = new XElement(leafName);
        parent.Add(leaf);
        return leaf;
    }

    private XElement GetOrCreateRoot(string name)
    {
        if (_document.Root is null)
        {
            var root = CreateDictionaryRoot();
            _document.Add(root);
        }

        var dictionaryRoot = _document.Root!;
        if (HasLocalName(dictionaryRoot, RootElementName) == false)
        {
            throw new InvalidOperationException(
                $"XML dictionary root is '{dictionaryRoot.Name.LocalName}', but '{RootElementName}' was expected."
            );
        }

        SetDictionaryRootAttributes(dictionaryRoot);

        var pathRoot = dictionaryRoot.Elements().FirstOrDefault(x => HasLocalName(x, name));
        if (pathRoot is not null)
        {
            return pathRoot;
        }

        pathRoot = new XElement(name);
        dictionaryRoot.Add(pathRoot);
        return pathRoot;
    }

    private XElement CreateDictionaryRoot()
    {
        var root = new XElement(RootElementName);
        SetDictionaryRootAttributes(root);
        return root;
    }

    private void SetDictionaryRootAttributes(XElement root)
    {
        root.SetAttributeValue(FormatAttributeName, FormatName);
        root.SetAttributeValue(VersionAttributeName, FormatVersion);
        root.SetAttributeValue(DescriptionAttributeName, _description);
    }

    private void RemoveTypedCore<TDto>(string[] segments)
    {
        var leaves = FindLeafCandidates(segments).Where(MatchesType<TDto>).ToArray();
        foreach (var leaf in leaves)
        {
            leaf.Remove();
        }

        _hasChanges |= leaves.Length > 0;
    }

    private static bool HasLocalName(XElement element, string name)
    {
        return string.Equals(element.Name.LocalName, name, StringComparison.Ordinal);
    }

    private static bool IsTypedElement(XElement element)
    {
        return element.Attribute(TypeAttributeName) is not null
            || element.Attribute(AssemblyAttributeName) is not null;
    }

    private static bool IsContainerElement(XElement element)
    {
        return element.HasElements && IsTypedElement(element) == false;
    }

    private static void SetStoredType<TDto>(XElement element)
    {
        var type = GetStorageType(typeof(TDto));
        element.SetAttributeValue(TypeAttributeName, GetTypeName(type));
        element.SetAttributeValue(AssemblyAttributeName, GetAssemblyName(type));
    }

    private static bool MatchesType<TDto>(XElement element)
    {
        var type = GetStorageType(typeof(TDto));
        var typeName = element.Attribute(TypeAttributeName)?.Value;
        var assemblyName = element.Attribute(AssemblyAttributeName)?.Value;

        return string.Equals(typeName, GetTypeName(type), StringComparison.Ordinal)
            && string.Equals(assemblyName, GetAssemblyName(type), StringComparison.Ordinal);
    }

    private static void WriteValue<TDto>(XElement element, TDto value)
    {
        if (value is null)
        {
            element.Value = string.Empty;
            return;
        }

        using var writer = element.CreateWriter();
        var serializer = new DataContractSerializer(GetStorageType(typeof(TDto)));
        serializer.WriteObject(writer, value);
    }

    private static bool TryReadValue<TDto>(XElement element, [MaybeNullWhen(false)] out TDto value)
    {
        var type = GetStorageType(typeof(TDto));
        try
        {
            var payload = element.Elements().FirstOrDefault();
            if (payload is null)
            {
                value = default;
                return false;
            }

            using var reader = payload.CreateReader();
            var serializer = new DataContractSerializer(type);
            var result = serializer.ReadObject(reader);
            if (result is TDto typedResult)
            {
                value = typedResult;
                return true;
            }

            var nullableType = Nullable.GetUnderlyingType(typeof(TDto));
            if (
                nullableType is not null
                && result is not null
                && nullableType.IsInstanceOfType(result)
            )
            {
                value = (TDto)result;
                return true;
            }

            if (result is null)
            {
                value = default;
                return false;
            }

            value = default;
            return false;
        }
        catch (Exception ex)
            when (ex
                    is FormatException
                        or InvalidCastException
                        or SerializationException
                        or XmlException
            )
        {
            value = default;
            return false;
        }
    }

    private static Type GetStorageType(Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    private static string GetTypeName(Type type)
    {
        return type.FullName ?? type.Name;
    }

    private static string GetAssemblyName(Type type)
    {
        return type.Assembly.GetName().Name ?? string.Empty;
    }
}
