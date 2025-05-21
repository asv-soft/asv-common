using System.Collections.Generic;

namespace Asv.IO;

public static class FieldMixin
{
    private const string TitleKey = "title";
    private const string DescriptionKey = "description";
    private const string UnitsKey = "units";
    private const string FormatStringKey = "format";
    
    public static string? GetDescription(this Field src) => src.Metadata.GetValueOrDefault(DescriptionKey) as string;
    public static string? GetUnits(this Field src) => src.Metadata.GetValueOrDefault(UnitsKey) as string;
    public static string? GetTitle(this Field src) => src.Metadata.GetValueOrDefault(TitleKey) as string;
    public static string? GetFormatString(this Field src) => src.Metadata.GetValueOrDefault(FormatStringKey) as string;
    
    public static Field.Builder Title(this Field.Builder src, string value)
    {
        src.Metadata(TitleKey, value);
        return src;
    }
        
    public static Field.Builder Description(this Field.Builder src,string value)
    {
        src.Metadata(DescriptionKey, value);
        return src;
    }

    public static Field.Builder Units(this Field.Builder src,string value)
    {
        src.Metadata(UnitsKey, value);
        return src;
    }

    public static Field.Builder FormatString(this Field.Builder src,string value)
    {
        src.Metadata(FormatStringKey, value);
        return src;
    }
}