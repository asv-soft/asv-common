using System;
using System.Buffers;

namespace Asv.IO;

/// <summary>
/// 'A': Subscription Message
///
/// Subscribe a message by name and give it an id that is used in Logged data Message.
/// This must come before the first corresponding Logged data Message.
/// </summary>
public class ULogSubscriptionMessageToken : IULogToken
{
    public static ULogToken Token => ULogToken.Subscription;
    public const string TokenName = "Subscription";
    public const byte TokenId = (byte)'A';

    public string Name => TokenName;
    public ULogToken Type => Token;
    public TokenPlaceFlags Section => TokenPlaceFlags.Definition | TokenPlaceFlags.Data;
    public SubscriptionMessageFields Fields { get; set; } = null!;

    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        Fields = new SubscriptionMessageFields
        {
            MultiId = BinSerialize.ReadByte(ref buffer),
            MessageId = BinSerialize.ReadUShort(ref buffer)
        };
        Fields.Deserialize(ref buffer);
        buffer = buffer[Fields.MessageName.Length..];
    }

    public void Serialize(ref Span<byte> buffer)
    {
        Fields.Serialize(ref buffer);
    }

    public int GetByteSize()
    {
        return Fields.GetByteSize();
    }
}

public class SubscriptionMessageFields : ISizedSpanSerializable
{
    private string _messageName;

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
            CheckName(value);
            _messageName = value;
        }
    }

    public int GetByteSize()
    {
        return sizeof(byte) + sizeof(ushort) + ULog.Encoding.GetByteCount(MessageName);
    }

    public void Deserialize(ref ReadOnlySpan<char> rawString)
    {
        MessageName = rawString.Trim().ToString();
    }

    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        var charSize = ULog.Encoding.GetCharCount(buffer);
        var charBuffer = new char[charSize];
        ULog.Encoding.GetChars(buffer, charBuffer);
        var rawString = new ReadOnlySpan<char>(charBuffer, 0, charSize);
        Deserialize(ref rawString);
    }

    public void Serialize(ref Span<byte> buffer)
    {
        CheckName(MessageName);
        BinSerialize.WriteByte(ref buffer, MultiId);
        BinSerialize.WriteUShort(ref buffer, MessageId);
        BinSerialize.WriteBlock(ref buffer, ULog.Encoding.GetBytes(MessageName));
    }

    private void CheckName(string? name)
    {
        ULog.CheckMessageName(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
    }
}