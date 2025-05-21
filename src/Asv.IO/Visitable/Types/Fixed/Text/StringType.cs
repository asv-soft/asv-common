namespace Asv.IO;

public sealed class StringType(EncodingId encoding, uint minSize, uint maxSize) : FieldType<StringType, string>
{
    public const int DefaultMinSize = 0;
    public const int DefaultMaxSize = 1024;
    public const string TypeId = "string";
    
    public static readonly StringType Ascii = new(EncodingId.Ascii, DefaultMinSize, DefaultMaxSize);
    public static readonly StringType Utf8 = new(EncodingId.Utf8, DefaultMinSize, DefaultMaxSize);
    public static readonly StringType Utf16 = new(EncodingId.Utf16, DefaultMinSize, DefaultMaxSize);
    public static readonly StringType Utf32 = new(EncodingId.Utf32, DefaultMinSize, DefaultMaxSize);
    
    public override string Name => TypeId;
    public EncodingId Encoding => encoding;
}

public enum EncodingId
{
    Ascii,
    Utf8,
    Utf16,
    Utf32
}