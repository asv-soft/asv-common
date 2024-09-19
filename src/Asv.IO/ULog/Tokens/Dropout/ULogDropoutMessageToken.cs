using System;

namespace Asv.IO;


/// <summary>
/// 'O': Dropout Message
///
/// Mark a dropout (lost logging messages) of a given duration in ms.
/// Dropouts can occur e.g. if the device is not fast enough.
/// </summary>
public class ULogDropoutMessageToken : IULogToken
{
    public static ULogToken Token => ULogToken.Dropout;
    public const string Name = "Dropout message";
    public const byte TokenId = (byte)'O';
    
    public string TokenName => Name;
    public ULogToken TokenType => Token;
    public TokenPlaceFlags TokenSection => TokenPlaceFlags.Data;

    /// <summary>
    /// Duration of the lost logging messages in ms.
    /// </summary>
    public ushort Duration { get; set; }

    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        Duration = BinSerialize.ReadUShort(ref buffer);
    }

    public void Serialize(ref Span<byte> buffer)
    {
        BinSerialize.WriteUShort(ref buffer, Duration);
    }

    public int GetByteSize()
    {
        return sizeof(ushort);
    }
}