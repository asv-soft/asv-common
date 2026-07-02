#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.IO.Packaging;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Asv.Store;

public class JsonDictionaryAsvPackage(
    Uri uriPart,
    AsvPackageContext context,
    AsvPackagePart? parent = null,
    string contentType = "application/json",
    CompressionOption compression = CompressionOption.Maximum,
    Encoding? encoding = null,
    string description = ""
) : DictionaryAsvPackage(uriPart, contentType, compression, context, parent)
{
    private const string RootPropertyName = "dictionary";
    private const string FormatPropertyName = "format";
    private const string VersionPropertyName = "version";
    private const string DescriptionPropertyName = "description";
    private const string EntriesPropertyName = "entries";
    private const string FormatName = "Asv.Store.Dictionary";
    private const string FormatVersion = "1";
    private const string TypePropertyName = "$type";
    private const string AssemblyPropertyName = "$assembly";
    private const string ValuePropertyName = "$value";
    private readonly string _description = description ?? string.Empty;
    private readonly JObject _document = LoadDocument(context, uriPart, contentType, encoding);
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
            var leaf = FindLeaf(segments);
            if (leaf is null || MatchesType<TDto>(leaf) == false)
            {
                return default;
            }

            return TryReadValue<TDto>(leaf, out var value) ? value : default;
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
            SetStoredValue(leaf, value);
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

            if (_document.HasValues == false)
            {
                _hasChanges = false;
                return;
            }

            var part = Context.Package.CreatePart(UriPart, ContentType, Compression);
            using var stream = part.GetStream(FileMode.Create, FileAccess.ReadWrite);
            using var streamWriter = new StreamWriter(
                stream,
                encoding ?? JsonPackageSettings.DefaultEncoding,
                leaveOpen: true
            );
            using var jsonWriter = new JsonTextWriter(streamWriter)
            {
                Formatting = Formatting.Indented,
                CloseOutput = false,
            };
            _document.WriteTo(jsonWriter);
            jsonWriter.Flush();
            streamWriter.Flush();
            _hasChanges = false;
        }
    }

    private static JObject LoadDocument(
        AsvPackageContext context,
        Uri uriPart,
        string contentType,
        Encoding? encoding
    )
    {
        if (
            context.Package.FileOpenAccess != FileAccess.Read
            && context.Package.FileOpenAccess != FileAccess.ReadWrite
        )
        {
            return new JObject();
        }

        using (context.Lock.EnterScope())
        {
            if (!context.Package.PartExists(uriPart))
            {
                return new JObject();
            }

            var packagePart = context.Package.GetPart(uriPart);
            if (packagePart.ContentType != contentType)
            {
                throw new InvalidOperationException(
                    $"Package part {uriPart} has content type '{packagePart.ContentType}', but '{contentType}' was expected."
                );
            }

            using var stream = packagePart.GetStream(FileMode.Open, FileAccess.Read);
            if (stream.Length == 0)
            {
                return new JObject();
            }

            using var streamReader = new StreamReader(
                stream,
                encoding ?? JsonPackageSettings.DefaultEncoding,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true
            );
            using var jsonReader = new JsonTextReader(streamReader);
            var document = JObject.Load(
                jsonReader,
                new JsonLoadSettings
                {
                    CommentHandling = CommentHandling.Ignore,
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                    LineInfoHandling = LineInfoHandling.Ignore,
                }
            );
            EnsureDictionaryRoot(document, uriPart);
            return document;
        }
    }

    private static void EnsureDictionaryRoot(JObject document, Uri uriPart)
    {
        var rootProperties = document.Properties().Take(2).ToArray();
        if (rootProperties.Length != 1)
        {
            throw new InvalidOperationException(
                $"JSON dictionary part {uriPart} must contain exactly one root property named '{RootPropertyName}'."
            );
        }

        var rootProperty = rootProperties[0];
        if (string.Equals(rootProperty.Name, RootPropertyName, StringComparison.Ordinal) == false)
        {
            throw new InvalidOperationException(
                $"JSON dictionary part {uriPart} root must be '{RootPropertyName}', but '{rootProperty.Name}' was found."
            );
        }

        if (rootProperty.Value is not JObject rootObject)
        {
            throw new InvalidOperationException(
                $"JSON dictionary part {uriPart} root '{RootPropertyName}' must be an object."
            );
        }

        if (
            string.Equals(
                rootObject.Value<string>(FormatPropertyName),
                FormatName,
                StringComparison.Ordinal
            ) == false
        )
        {
            throw new InvalidOperationException(
                $"JSON dictionary part {uriPart} must have '{FormatPropertyName}' property equal to '{FormatName}'."
            );
        }

        if (
            string.Equals(
                rootObject.Value<string>(VersionPropertyName),
                FormatVersion,
                StringComparison.Ordinal
            ) == false
        )
        {
            throw new InvalidOperationException(
                $"JSON dictionary part {uriPart} must have '{VersionPropertyName}' property equal to '{FormatVersion}'."
            );
        }

        if (
            rootObject.TryGetValue(EntriesPropertyName, StringComparison.Ordinal, out var entries)
            && entries is not JObject
        )
        {
            throw new InvalidOperationException(
                $"JSON dictionary part {uriPart} '{EntriesPropertyName}' property must be an object."
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
                "Dictionary path must contain at least one JSON property name.",
                nameof(path)
            );
        }

        foreach (var segment in segments)
        {
            if (IsReservedMetadataPropertyName(segment))
            {
                throw new ArgumentException(
                    $"Dictionary path segment '{segment}' is reserved for JSON dictionary metadata.",
                    nameof(path)
                );
            }
        }

        return segments;
    }

    private static bool IsReservedMetadataPropertyName(string name)
    {
        return string.Equals(name, TypePropertyName, StringComparison.Ordinal)
            || string.Equals(name, AssemblyPropertyName, StringComparison.Ordinal)
            || string.Equals(name, ValuePropertyName, StringComparison.Ordinal);
    }

    private JObject? FindLeaf(string[] segments)
    {
        var root = FindRoot(segments[0]);
        if (root is null)
        {
            return null;
        }

        if (segments.Length == 1)
        {
            return root;
        }

        var current = root;
        for (var i = 1; i < segments.Length; i++)
        {
            var isLeaf = i == segments.Length - 1;
            if (
                current.TryGetValue(segments[i], StringComparison.Ordinal, out var next) == false
                || next is not JObject nextObject
            )
            {
                return null;
            }

            if (isLeaf == false && IsTypedObject(nextObject))
            {
                return null;
            }

            current = nextObject;
        }

        return current;
    }

    private JObject GetOrCreateLeaf<TDto>(string[] segments)
    {
        if (segments.Length == 1)
        {
            var root = GetOrCreateRoot(segments[0]);
            if (IsContainerObject(root))
            {
                throw new InvalidOperationException(
                    $"Dictionary path '{segments[0]}' is already used as a container."
                );
            }

            return root;
        }

        var parent = GetOrCreateRoot(segments[0]);
        if (IsTypedObject(parent))
        {
            throw new InvalidOperationException(
                $"Dictionary path '{segments[0]}' is already used as a value."
            );
        }

        for (var i = 1; i < segments.Length - 1; i++)
        {
            var segment = segments[i];
            if (parent.TryGetValue(segment, StringComparison.Ordinal, out var next))
            {
                if (next is not JObject nextObject)
                {
                    throw new InvalidOperationException(
                        $"Dictionary path segment '{segment}' is already used as a scalar value."
                    );
                }

                if (IsTypedObject(nextObject))
                {
                    throw new InvalidOperationException(
                        $"Dictionary path segment '{segment}' is already used as a value."
                    );
                }

                parent = nextObject;
                continue;
            }

            var created = new JObject();
            parent.Add(segment, created);
            parent = created;
        }

        var leafName = segments[^1];
        if (parent.TryGetValue(leafName, StringComparison.Ordinal, out var leaf))
        {
            if (leaf is not JObject leafObject)
            {
                throw new InvalidOperationException(
                    $"Dictionary path '{string.Join("/", segments)}' is already used as a scalar value."
                );
            }

            if (IsContainerObject(leafObject))
            {
                throw new InvalidOperationException(
                    $"Dictionary path '{string.Join("/", segments)}' is already used as a container."
                );
            }

            return leafObject;
        }

        var createdLeaf = new JObject();
        parent.Add(leafName, createdLeaf);
        return createdLeaf;
    }

    private JObject? FindRoot(string name)
    {
        var entries = FindEntries();
        if (entries is null)
        {
            return null;
        }

        if (
            entries.TryGetValue(name, StringComparison.Ordinal, out var value) == false
            || value is not JObject rootObject
        )
        {
            return null;
        }

        return rootObject;
    }

    private JObject GetOrCreateRoot(string name)
    {
        var entries = GetOrCreateEntries();
        if (entries.TryGetValue(name, StringComparison.Ordinal, out var value))
        {
            if (value is not JObject rootObject)
            {
                throw new InvalidOperationException(
                    $"JSON dictionary root '{name}' must be an object."
                );
            }

            return rootObject;
        }

        var createdRoot = new JObject();
        entries.Add(name, createdRoot);
        return createdRoot;
    }

    private JObject? FindEntries()
    {
        var dictionaryRoot = FindDictionaryRoot();
        if (dictionaryRoot is null)
        {
            return null;
        }

        return dictionaryRoot[EntriesPropertyName] as JObject;
    }

    private JObject GetOrCreateEntries()
    {
        var dictionaryRoot = GetOrCreateDictionaryRoot();
        if (dictionaryRoot.TryGetValue(EntriesPropertyName, StringComparison.Ordinal, out var value))
        {
            if (value is not JObject entries)
            {
                throw new InvalidOperationException(
                    $"JSON dictionary '{EntriesPropertyName}' property must be an object."
                );
            }

            return entries;
        }

        var created = new JObject();
        dictionaryRoot.Add(EntriesPropertyName, created);
        return created;
    }

    private JObject? FindDictionaryRoot()
    {
        if (_document.TryGetValue(RootPropertyName, StringComparison.Ordinal, out var root) == false)
        {
            return null;
        }

        return root as JObject;
    }

    private JObject GetOrCreateDictionaryRoot()
    {
        if (_document.TryGetValue(RootPropertyName, StringComparison.Ordinal, out var value))
        {
            if (value is not JObject root)
            {
                throw new InvalidOperationException(
                    $"JSON dictionary root '{RootPropertyName}' must be an object."
                );
            }

            SetDictionaryRootProperties(root);
            return root;
        }

        if (_document.HasValues)
        {
            throw new InvalidOperationException(
                $"JSON dictionary root must be '{RootPropertyName}'."
            );
        }

        var created = new JObject();
        SetDictionaryRootProperties(created);
        _document.Add(RootPropertyName, created);
        return created;
    }

    private void SetDictionaryRootProperties(JObject root)
    {
        root[FormatPropertyName] = FormatName;
        root[VersionPropertyName] = FormatVersion;
        root[DescriptionPropertyName] = _description;
    }

    private void RemoveTypedCore<TDto>(string[] segments)
    {
        var leaf = FindLeaf(segments);
        if (leaf is null || MatchesType<TDto>(leaf) == false)
        {
            return;
        }

        RemoveLeaf(leaf);
        _hasChanges = true;
    }

    private static void RemoveLeaf(JObject leaf)
    {
        if (leaf.Parent is JProperty property)
        {
            property.Remove();
            return;
        }

        leaf.Remove();
    }

    private static bool IsTypedObject(JObject obj)
    {
        return obj.Property(TypePropertyName) is not null
            || obj.Property(AssemblyPropertyName) is not null;
    }

    private static bool IsContainerObject(JObject obj)
    {
        return obj.HasValues && IsTypedObject(obj) == false;
    }

    private static void SetStoredValue<TDto>(JObject leaf, TDto value)
    {
        var type = GetStorageType(typeof(TDto));
        leaf.RemoveAll();
        leaf[TypePropertyName] = GetTypeName(type);
        leaf[AssemblyPropertyName] = GetAssemblyName(type);
        leaf[ValuePropertyName] = WriteValue(value);
    }

    private static bool MatchesType<TDto>(JObject leaf)
    {
        var type = GetStorageType(typeof(TDto));
        var typeName = leaf.Value<string>(TypePropertyName);
        var assemblyName = leaf.Value<string>(AssemblyPropertyName);

        return string.Equals(typeName, GetTypeName(type), StringComparison.Ordinal)
            && string.Equals(assemblyName, GetAssemblyName(type), StringComparison.Ordinal);
    }

    private static JToken WriteValue<TDto>(TDto value)
    {
        using var stream = new MemoryStream();
        var serializer = new DataContractJsonSerializer(GetStorageType(typeof(TDto)));
        serializer.WriteObject(stream, value);
        stream.Position = 0;

        using var streamReader = new StreamReader(
            stream,
            JsonPackageSettings.DefaultEncoding,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true
        );
        using var jsonReader = new JsonTextReader(streamReader);
        return JToken.ReadFrom(jsonReader);
    }

    private static bool TryReadValue<TDto>(JObject leaf, [MaybeNullWhen(false)] out TDto value)
    {
        var valueToken = leaf[ValuePropertyName];
        if (valueToken is null || valueToken.Type == JTokenType.Null)
        {
            value = default;
            return false;
        }

        var type = GetStorageType(typeof(TDto));
        try
        {
            var json = valueToken.ToString(Formatting.None);
            var bytes = JsonPackageSettings.DefaultEncoding.GetBytes(json);
            using var stream = new MemoryStream(bytes);
            var serializer = new DataContractJsonSerializer(type);
            var result = serializer.ReadObject(stream);
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

            value = default;
            return false;
        }
        catch (Exception ex)
            when (ex
                    is FormatException
                        or InvalidCastException
                        or SerializationException
                        or JsonException
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
