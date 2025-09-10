namespace Asv.IO;

public sealed class CharOptionalType(EncodingId encoding, string? allowedChars = null)
    : FixedType<CharOptionalType, char?>
{
    public const string TypeId = "char?";

    public static readonly CharType Ascii = new(EncodingId.Ascii);
    public static readonly CharType Utf8 = new(EncodingId.Utf8);
    public static readonly CharType Utf16 = new(EncodingId.Utf16);
    public static readonly CharType Utf32 = new(EncodingId.Utf32);

    public override string Name => TypeId;
    public EncodingId Encoding => encoding;
    public string? AllowedChars => allowedChars;
}
