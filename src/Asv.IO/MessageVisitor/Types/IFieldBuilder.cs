using System.Collections.Immutable;

namespace Asv.IO.MessageVisitor;

public interface IFieldBuilder<out TSelf, out TField>
    where TSelf: IFieldBuilder<TSelf, TField>
    where TField: IField
{
    TSelf MySelf { get; }
    ImmutableDictionary<string,object?>.Builder Metadata { get; }
    TSelf Name(string name);
    TField Build();
}

public static class FieldMetadataMixin
{
    private const string TitleKey = "title";
    private const string DescriptionKey = "description";
    private const string UnitsKey = "units";
    private const string FormatStringKey = "format";
    
    public static string? GetDescription(this Field src) => src.Metadata.GetValueOrDefault(DescriptionKey);
    public static string? GetUnits(this Field src) => src.Metadata.GetValueOrDefault(UnitsKey);
    public static string? GetTitle(this Field src) => src.Metadata.GetValueOrDefault(TitleKey);
    public static string? GetFormatString(this Field src) => src.Metadata.GetValueOrDefault(FormatStringKey);
    
    public static TSelf Title<TSelf, TField>(this IFieldBuilder<TSelf, TField> src, string value) 
        where TSelf : IFieldBuilder<TSelf, TField> 
        where TField : IField
    {
        src.Metadata[TitleKey] = value;
        return src.MySelf;
    }
        
    public static TSelf Description<TSelf, TField>(this IFieldBuilder<TSelf, TField> src, string value) 
        where TSelf : IFieldBuilder<TSelf, TField> 
        where TField : IField
    {
        src.Metadata[DescriptionKey] = value;
        return src.MySelf;
    }

    public static TSelf Units<TSelf, TField>(this IFieldBuilder<TSelf, TField> src, string value) 
        where TSelf : IFieldBuilder<TSelf, TField> 
        where TField : IField
    {
        src.Metadata[UnitsKey] = value;
        return src.MySelf;
    }

    public static TSelf FormatString<TSelf, TField>(this IFieldBuilder<TSelf, TField> src, string value) 
        where TSelf : IFieldBuilder<TSelf, TField> 
        where TField : IField
    {
        src.Metadata[FormatStringKey] = value;
        return src.MySelf;
    }
}