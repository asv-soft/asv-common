using System;

namespace Asv.IO;

/// <summary>
/// 'R': Unsubscription Message
///
/// Unsubscribe a message, to mark that it will not be logged anymore (not used currently).
/// </summary>
public class ULogUnsubscriptionMessageToken : IULogToken
{
    #region Static

    public static ULogToken Type => ULogToken.Unsubscription;
    public const string Name = "Unsubscription";
    public const byte TokenId = (byte)'R';

    #endregion
    
    public string TokenName => Name;
    public ULogToken TokenType => Type;
    public TokenPlaceFlags TokenSection => TokenPlaceFlags.Data;

    /// <summary>
    /// ID of the message
    /// </summary>
    public ushort MessageId;
    
    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        MessageId = BinSerialize.ReadUShort(ref buffer);
    }

    public void Serialize(ref Span<byte> buffer)
    {
        BinSerialize.WriteUShort(ref buffer, MessageId);
    }

    public int GetByteSize()
    {
        return sizeof(ushort)/*MessageId*/;
    }
}