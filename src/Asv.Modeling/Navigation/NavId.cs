using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Asv.Modeling;

public readonly partial struct NavId : IEquatable<NavId>
{
    public const char Separator = '?';

    private const string TypeIdRegexString = "^[a-zA-Z0-9\\._\\-]+$";

    [GeneratedRegex(TypeIdRegexString, RegexOptions.Compiled)]
    private static partial Regex CreateTypeIdRegex();

    private static readonly Regex TypeIdRegex = CreateTypeIdRegex();

    #region Generation

    private static readonly JsonSerializerOptions StableJsonOptions = new()
    {
        WriteIndented = false,
    };

    private const string AllowedCharacters =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-.";

    public static string GenerateRandomAsString(int length = 16, Random? random = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        if (length == 0)
        {
            return string.Empty;
        }

        return string.Create(
            length,
            random,
            static (span, rng) =>
            {
                for (var i = 0; i < span.Length; i++)
                {
                    var index = rng == null
                        ? RandomNumberGenerator.GetInt32(AllowedCharacters.Length)
                        : rng.Next(AllowedCharacters.Length);
                    span[i] = AllowedCharacters[index];
                }
            }
        );
    }

    public static NavId GenerateByHash<T1>(T1 value1)
    {
        return new NavId(GenerateByHashAsString(value1), default);
    }

    public static NavId GenerateByHash<T1, T2>(T1 value1, T2 value2)
    {
        return new NavId(GenerateByHashAsString(value1, value2), default);
    }

    public static NavId GenerateByHash<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
    {
        return new NavId(GenerateByHashAsString(value1, value2, value3), default);
    }

    public static NavId GenerateByHash<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
    {
        return new NavId(GenerateByHashAsString(value1, value2, value3, value4), default);
    }

    public static NavId GenerateByHash<T1, T2, T3, T4, T5>(
        T1 value1,
        T2 value2,
        T3 value3,
        T4 value4,
        T5 value5
    )
    {
        return new NavId(GenerateByHashAsString(value1, value2, value3, value4, value5), default);
    }

    public static string GenerateByHashAsString<T1>(T1 value1)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendValue(hash, value1);
        return ToStableTypeId(hash.GetHashAndReset());
    }

    public static string GenerateByHashAsString<T1, T2>(T1 value1, T2 value2)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendValue(hash, value1);
        AppendValue(hash, value2);
        return ToStableTypeId(hash.GetHashAndReset());
    }

    public static string GenerateByHashAsString<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendValue(hash, value1);
        AppendValue(hash, value2);
        AppendValue(hash, value3);
        return ToStableTypeId(hash.GetHashAndReset());
    }

    public static string GenerateByHashAsString<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendValue(hash, value1);
        AppendValue(hash, value2);
        AppendValue(hash, value3);
        AppendValue(hash, value4);
        return ToStableTypeId(hash.GetHashAndReset());
    }

    public static string GenerateByHashAsString<T1, T2, T3, T4, T5>(
        T1 value1,
        T2 value2,
        T3 value3,
        T4 value4,
        T5 value5
    )
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendValue(hash, value1);
        AppendValue(hash, value2);
        AppendValue(hash, value3);
        AppendValue(hash, value4);
        AppendValue(hash, value5);
        return ToStableTypeId(hash.GetHashAndReset());
    }
    
    private static void AppendValue<T>(IncrementalHash hash, T value)
    {
        if (value == null)
        {
            hash.AppendData([0]);
            return;
        }

        hash.AppendData([1]);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, StableJsonOptions);
        Span<byte> lengthBytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(lengthBytes, bytes.Length);
        hash.AppendData(lengthBytes);
        hash.AppendData(bytes);
    }

    #endregion

    public NavId(string typeId, NavArgs args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeId);
        if (!TypeIdRegex.IsMatch(typeId))
        {
            throw new ArgumentException(
                $"{nameof(typeId)} must match regex '{TypeIdRegexString}'.",
                nameof(typeId)
            );
        }

        TypeId = typeId;
        Args = args;
    }

    public NavId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var separatorIndex = value.IndexOf(Separator);
        if (separatorIndex < 0)
        {
            this = new NavId(value, default);
            return;
        }

        var typeId = value[..separatorIndex];
        var args = separatorIndex == value.Length - 1
            ? default
            : NavArgs.Parse(value[(separatorIndex + 1)..]);
        this = new NavId(typeId, args);
    }

    public string TypeId { get; } = string.Empty;

    public NavArgs Args { get; }

    public bool Equals(NavId other) =>
        string.Equals(TypeId, other.TypeId, StringComparison.OrdinalIgnoreCase) && Args.Equals(other.Args);

    public override bool Equals(object? obj) => obj is NavId other && Equals(other);

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(TypeId, StringComparer.OrdinalIgnoreCase);
        hash.Add(Args);
        return hash.ToHashCode();
    }

    public static bool operator ==(NavId left, NavId right) => left.Equals(right);

    public static bool operator !=(NavId left, NavId right) => !left.Equals(right);

    public override string ToString()
    {
        return Args.IsEmpty ? TypeId : $"{TypeId}{Separator}{Args}";
    }

    

    private static string ToStableTypeId(byte[] hash)
    {
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
