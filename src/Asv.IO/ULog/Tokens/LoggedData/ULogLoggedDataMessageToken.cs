using System;

namespace Asv.IO;

/// <summary>
/// 'D': Logged Data Message 
/// </summary>
public class ULogLoggedDataMessageToken : IULogToken
{
    #region Static

    public const string Name = "LoggedData";
    public static ULogToken Type => ULogToken.LoggedData;
    public const byte TokenId = (byte)'D';

    #endregion
    
    public string TokenName => Name;
    public ULogToken TokenType => Type;
    public TokenPlaceFlags TokenSection => TokenPlaceFlags.Data;
    
    /// <summary>
    /// msg_id: unique id to match Logged data Message data. The first use must set this to 0, then increase it.
    /// 
    /// The same msg_id must not be used twice for different subscriptions.
    /// </summary>
    public ushort MessageId { get; set; }

    /// <summary>
    /// data contains the logged binary message as defined by Format Message
    /// </summary>
    public byte[] Data { get; set; } = [];
    
    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        MessageId = BinSerialize.ReadUShort(ref buffer);
        Data = BinSerialize.ReadBlock(ref buffer, buffer.Length);
    }

    public void Serialize(ref Span<byte> buffer)
    {
        BinSerialize.WriteUShort(ref buffer, MessageId);
        BinSerialize.WriteBlock(ref buffer, Data);
    }

    public int GetByteSize()
    {
        return sizeof(ushort) /*msg_id*/ + Data.Length /*data*/ ;
    }
}