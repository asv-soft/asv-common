using System;

namespace Asv.IO;

/// <summary>
/// 'F': Format Message
///
/// Format message defines a single message name and its inner fields in a single string.
/// </summary>
public class ULogMessageFormat: IULogToken
{
    public static ULogToken Token => ULogToken.Format;
    public const string TokenName = "Format";
    public const byte TokenId = (byte)'F';
    public string Name => TokenName;
    public ULogToken Type => Token;
    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public void Serialize(ref Span<byte> buffer)
    {
        throw new NotImplementedException();
    }
}