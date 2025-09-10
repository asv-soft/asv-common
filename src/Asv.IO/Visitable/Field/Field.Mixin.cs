using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Asv.IO;

public record EnumValue<T>
{
    public EnumValue(T value, string name, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(name);
        Value = value;
        Name = name;
        Description = description;
    }

    public T Value { get; }
    public string Name { get; }
    public string? Description { get; }
}

public static class FieldMixin
{
    private const string TitleKey = "title";
    private const string DescriptionKey = "description";
    private const string UnitsKey = "units";
    private const string FormatStringKey = "format";
    private const string EnumKey = "enum";

    public static string? GetDescription(this Field src) =>
        CollectionExtensions.GetValueOrDefault(src.Metadata, DescriptionKey) as string;

    public static string? GetUnits(this Field src) =>
        CollectionExtensions.GetValueOrDefault(src.Metadata, UnitsKey) as string;

    public static string? GetTitle(this Field src) =>
        CollectionExtensions.GetValueOrDefault(src.Metadata, TitleKey) as string;

    public static string? GetFormatString(this Field src) =>
        CollectionExtensions.GetValueOrDefault(src.Metadata, FormatStringKey) as string;

    public static ImmutableDictionary<T, EnumValue<T>> GetEnum<T>(this Field src)
        where T : notnull
    {
        return CollectionExtensions.GetValueOrDefault(src.Metadata, EnumKey)
                as ImmutableDictionary<T, EnumValue<T>>
            ?? ImmutableDictionary<T, EnumValue<T>>.Empty;
    }

    public static Field.Builder Title(this Field.Builder src, string value)
    {
        src.Metadata(TitleKey, value);
        return src;
    }

    public static Field.Builder Description(this Field.Builder src, string value)
    {
        src.Metadata(DescriptionKey, value);
        return src;
    }

    public static Field.Builder Units(this Field.Builder src, string value)
    {
        src.Metadata(UnitsKey, value);
        return src;
    }

    public static Field.Builder FormatString(this Field.Builder src, string value)
    {
        src.Metadata(FormatStringKey, value);
        return src;
    }

    public static Field.Builder Enum<T>(
        this Field.Builder src,
        params IEnumerable<EnumValue<T>> values
    )
    {
        src.Metadata(EnumKey, values.ToImmutableDictionary(x => x.Name));
        return src;
    }
}
