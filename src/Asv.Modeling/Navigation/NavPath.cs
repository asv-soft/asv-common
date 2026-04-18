using System.Collections;
using System.Text;

namespace Asv.Modeling;

public readonly struct NavPath : IEquatable<NavPath>, IEnumerable<NavId>
{
    public const char Separator = '/';

    private readonly NavId[]? _items;

    public NavPath(params NavId[] items)
    {
        _items = items is { Length: > 0 } ? items.ToArray() : null;
    }

    public NavPath(IEnumerable<NavId> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _items = items.ToArray();
        if (_items.Length == 0)
        {
            _items = null;
        }
    }

    public int Count => _items?.Length ?? 0;

    public bool IsEmpty => Count == 0;

    public NavId this[int index] => (_items ?? Array.Empty<NavId>())[index];

    public static NavPath Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        var parts = value.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        var items = new NavId[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            items[i] = new NavId(parts[i]);
        }

        return new NavPath(items);
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
                sb.Append(Separator);
            }

            sb.Append(_items![i]);
        }

        return sb.ToString();
    }

    public bool Equals(NavPath other)
    {
        if (Count != other.Count)
        {
            return false;
        }

        for (var i = 0; i < Count; i++)
        {
            if (_items![i] != other._items![i])
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is NavPath other && Equals(other);

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        if (_items == null)
        {
            return hash.ToHashCode();
        }

        foreach (var item in _items)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(NavPath left, NavPath right) => left.Equals(right);

    public static bool operator !=(NavPath left, NavPath right) => !left.Equals(right);

    public IEnumerator<NavId> GetEnumerator() =>
        ((_items ?? Array.Empty<NavId>()) as IEnumerable<NavId>).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
