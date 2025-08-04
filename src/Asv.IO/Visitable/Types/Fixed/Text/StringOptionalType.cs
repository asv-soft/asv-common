namespace Asv.IO;

public sealed class StringOptionalType(EncodingId encoding, uint minSize, uint maxSize, string? allowedChars = null) 
    : FieldType<StringOptionalType, string?>
{
    
    public const int DefaultMinSize = 0;
    public const int DefaultMaxSize = 1024;
    public const string TypeId = "string?";
    
    public static readonly StringOptionalType Ascii = new(EncodingId.Ascii, DefaultMinSize, DefaultMaxSize);
    public static readonly StringOptionalType Utf8 = new(EncodingId.Utf8, DefaultMinSize, DefaultMaxSize);
    public static readonly StringOptionalType Utf16 = new(EncodingId.Utf16, DefaultMinSize, DefaultMaxSize);
    public static readonly StringOptionalType Utf32 = new(EncodingId.Utf32, DefaultMinSize, DefaultMaxSize);
    
    public override string Name => TypeId;
    public EncodingId Encoding => encoding;

    public uint MinSize { get; } = minSize;
    public uint MaxSize { get; } = maxSize;
    public string? AllowedChars { get; } = allowedChars;
}