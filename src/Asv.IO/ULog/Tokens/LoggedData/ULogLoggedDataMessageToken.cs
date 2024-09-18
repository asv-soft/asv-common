using System;
using System.Security.AccessControl;

namespace Asv.IO;

/// <summary>
/// 'D': Logged Data Message 
/// </summary>
public class ULogLoggedDataMessageToken : ULogKeyAndValueTokenBase
{
    #region Static

    public const string Name = "LoggedData";
    public static ULogToken Type => ULogToken.LoggedData;
    public const byte TokenId = (byte)'D';

    #endregion
    
    public override string TokenName => TokenName;
    public override ULogToken TokenType => Type;
    public override TokenPlaceFlags TokenSection => TokenPlaceFlags.Data;
    
    /// <summary>
    /// msg_id: unique id to match Logged data Message data. The first use must set this to 0, then increase it.
    /// 
    /// The same msg_id must not be used twice for different subscriptions.
    /// </summary>
    public ushort MessageId { get; set; }

    /// <summary>
    /// data contains the logged binary message as defined by Format Message
    /// </summary>
    public ULogTypeAndNameDefinition Data { get; set; } = new();
    
    public override void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        MessageId = BinSerialize.ReadUShort(ref buffer);
        Data.Deserialize(ref buffer);
    }

    public override void Serialize(ref Span<byte> buffer)
    {
        BinSerialize.WriteUShort(ref buffer, MessageId);
        Data.Serialize(ref buffer);
    }

    public override int GetByteSize()
    {
        return 2 + Data.GetByteSize();
    }
}