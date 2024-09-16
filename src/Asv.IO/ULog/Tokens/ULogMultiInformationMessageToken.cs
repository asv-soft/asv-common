using System;

namespace Asv.IO;

/// <summary>
/// 'M': Multi Information Message
/// Multi information message serves the same purpose as the information message, but for long messages or multiple messages with the same key.
/// </summary>
public class ULogMultiInformationMessageToken : IULogToken
{
    private static ULogToken Token => ULogToken.MultiInformation;
    private const string TokenName = "MultiInformation";
    public const byte TokenId = (byte)'M';

    public string Name => TokenName;
    public ULogToken Type => Token;
    public TokenPlaceFlags Section => TokenPlaceFlags.Definition| TokenPlaceFlags.Data;

    /// <summary>
    /// IsContinued can be used for split-up messages: if set to 1, it is part of the previous message with the same key.
    /// </summary>
    public bool IsContinued { get; set; }

    /// <summary>
    /// Contains InformationMessageToken implementation of a 
    /// </summary>
    public ULogInformationMessageToken InformationMessage { get; set; } = new();

    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        int isContinued = BinSerialize.ReadByte(ref buffer);
        if (isContinued == 1) IsContinued = true;
        InformationMessage.Deserialize(ref buffer);
    }

    public void Serialize(ref Span<byte> buffer)
    {
        BinSerialize.WriteByte(ref buffer, IsContinued ? (byte)1 : (byte)0);
        InformationMessage.Serialize(ref buffer);
    }

    public int GetByteSize()
    {
        return sizeof(bool) + InformationMessage.GetByteSize();
    }
}