using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Asv.Modeling;

public readonly partial struct NavArgs
    : IEquatable<NavArgs>,
        IEnumerable<KeyValuePair<string, string?>>
{
    public const char ArgSeparator = '=';
    public const char KeyValueSeparator = '&';
    public const string ArgKeyRegexString = "^[a-zA-Z0-9_-]+$";
    public static NavArgs Empty => default;

    [GeneratedRegex(ArgKeyRegexString, RegexOptions.Compiled)]
    private static partial Regex CreateArgKeyRegex();

    private static readonly Regex ArgKeyRegex = CreateArgKeyRegex();

    private readonly KeyValuePair<string, string?>[]? _items;

    public NavArgs(params KeyValuePair<string, string?>[] args)
    {
        _items = args is { Length: > 0 } ? Normalize(args) : null;
    }

    public NavArgs(IEnumerable<KeyValuePair<string, string?>> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        _items = Normalize(args).ToArray();
        if (_items.Length == 0)
        {
            _items = null;
        }
    }

    public int Count => _items?.Length ?? 0;

    public bool IsEmpty => Count == 0;

    public KeyValuePair<string, string?> this[int index] => (_items ?? Array.Empty<KeyValuePair<string, string?>>())[index];

    public static NavArgs Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        var parts = value.Split(KeyValueSeparator, StringSplitOptions.RemoveEmptyEntries);
        var result = new KeyValuePair<string, string?>[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var separatorIndex = part.IndexOf(ArgSeparator);
            if (separatorIndex <= 0 || separatorIndex != part.LastIndexOf(ArgSeparator))
            {
                throw new ArgumentException(
                    $"Argument '{part}' must be in 'key{ArgSeparator}UrlEncode(value)' format.",
                    nameof(value)
                );
            }

            var key = part[..separatorIndex];
            ValidateKey(key);
            var encodedValue = part[(separatorIndex + 1)..];
            result[i] = new KeyValuePair<string, string?>(key, HttpUtility.UrlDecode(encodedValue));
        }

        return new NavArgs(result);
    }

    public override string ToString()
    {
        if (IsEmpty)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < Count; i++)
        {
            if (i > 0)
            {
                sb.Append(KeyValueSeparator);
            }

            var item = _items![i];
            sb.Append(item.Key);
            sb.Append(ArgSeparator);
            sb.Append(HttpUtility.UrlEncode(item.Value ?? string.Empty));
        }

        return sb.ToString();
    }

    public bool Equals(NavArgs other)
    {
        if (Count != other.Count)
        {
            return false;
        }

        for (var i = 0; i < Count; i++)
        {
            var left = _items![i];
            var right = other._items![i];
            if (!string.Equals(left.Key, right.Key, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(left.Value, right.Value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is NavArgs other && Equals(other);

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        if (_items == null)
        {
            return hash.ToHashCode();
        }

        foreach (var item in _items)
        {
            hash.Add(item.Key, StringComparer.Ordinal);
            hash.Add(item.Value, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(NavArgs left, NavArgs right) => left.Equals(right);

    public static bool operator !=(NavArgs left, NavArgs right) => !left.Equals(right);

    public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() =>
        ((_items ?? Array.Empty<KeyValuePair<string, string?>>()) as IEnumerable<KeyValuePair<string, string?>>).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static IEnumerable<KeyValuePair<string, string?>> Normalize(
        IEnumerable<KeyValuePair<string, string?>> args
    )
    {
        foreach (var item in args)
        {
            ValidateKey(item.Key);
            yield return new KeyValuePair<string, string?>(item.Key, item.Value);
        }
    }

    private static KeyValuePair<string, string?>[] Normalize(
        KeyValuePair<string, string?>[] args
    )
    {
        var result = new KeyValuePair<string, string?>[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            ValidateKey(args[i].Key);
            result[i] = new KeyValuePair<string, string?>(args[i].Key, args[i].Value);
        }

        return result;
    }

    private static void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!ArgKeyRegex.IsMatch(key))
        {
            throw new ArgumentException(
                $"{nameof(key)} must match regex '{ArgKeyRegexString}'.",
                nameof(key)
            );
        }
    }
}
