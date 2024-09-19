using System;
using System.Buffers;
using System.Drawing;

namespace Asv.IO;

/// <summary>
/// 'A': Subscription Message
///
/// Subscribe a message by name and give it an id that is used in Logged data Message.
/// This must come before the first corresponding Logged data Message.
/// </summary>
public class ULogSubscriptionMessageToken : IULogToken
{
    public static ULogToken Type = ULogToken.Subscription;
    public const string Name = "Subscription";
    public const byte TokenId = (byte)'A';
   
    private string _messageName = null!;
    public string TokenName => Name;
    public ULogToken TokenType => Type;
    public TokenPlaceFlags TokenSection => TokenPlaceFlags.Definition | TokenPlaceFlags.Data;
    
    /// <summary>
    /// The same message format can have multiple instances, for example if the system has two sensors of the same type.
    /// 
    /// The default and first instance must be 0.
    /// </summary>
    public byte MultiId { get; set; }

    /// <summary>
    /// Unique id to match Logged data Message data. The first use must set this to 0, then increase it.
    ///
    /// The same msg_id must not be used twice for different subscriptions.
    /// </summary>
    public ushort MessageId { get; set; }

    /// <summary>
    /// Message name to subscribe to. Must match one of the Format Message definitions.
    /// </summary>
    public string MessageName
    {
        get => _messageName;
        set
        {
            ULogFormatMessageToken.CheckMessageName(value);;
            _messageName = value;
        }
    }
    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        MultiId = BinSerialize.ReadByte(ref buffer);
        MessageId = BinSerialize.ReadUShort(ref buffer);
        var charSize = ULog.Encoding.GetCharCount(buffer);
        var charBuffer = ArrayPool<char>.Shared.Rent(charSize);
        try
        {
            ULog.Encoding.GetChars(buffer, charBuffer);
            MessageName = new ReadOnlySpan<char>(charBuffer, 0, charSize).Trim().ToString();
            buffer = buffer[MessageName.Length..];
        }
        finally
        { 
            ArrayPool<char>.Shared.Return(charBuffer);
        }
        
    }

    public void Serialize(ref Span<byte> buffer)
    {
        ULogFormatMessageToken.CheckMessageName(MessageName);
        BinSerialize.WriteByte(ref buffer, MultiId);
        BinSerialize.WriteUShort(ref buffer, MessageId);
        BinSerialize.WriteBlock(ref buffer, ULog.Encoding.GetBytes(MessageName));
    }

    public int GetByteSize()
    {
        return sizeof(byte) + sizeof(ushort) + ULog.Encoding.GetByteCount(MessageName);
    }

    
}