using System;
using System.Buffers;

namespace Asv.IO;

/// <summary>
/// 'A': Subscription Message
///
/// Subscribe a message by name and give it an id that is used in Logged data Message.
/// This must come before the first corresponding Logged data Message.
/// </summary>
public class ULogSubscriptionMessageToken: IULogToken
{
    public static ULogToken Token => ULogToken.Subscription;
    public const string TokenName = "Subscription";
    public const byte TokenId = (byte)'A';

    public string Name => TokenName;
    public ULogToken Type => Token;
    public TokenPlaceFlags Section => TokenPlaceFlags.Definition | TokenPlaceFlags.Data;
    public SubscriptionMessageField Fields { get; set; } = null!;
    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        Fields = new SubscriptionMessageField
        {
            MultiId = BinSerialize.ReadByte(ref buffer),
            MessageId = BinSerialize.ReadShort(ref buffer)
        };
        Fields.Deserialize(ref buffer);
        buffer = buffer[Fields.MessageName.Length..];
    }

    public void Serialize(ref Span<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public int GetByteSize()
    {
        throw new NotImplementedException();
    }
}

public class SubscriptionMessageField : ISizedSpanSerializable
{
    private string _messageName;
    
    public byte MultiId { get; set; }
    public Int16 MessageId { get; set; }
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
        return ULog.Encoding.GetByteCount(MessageName) + MessageId + MultiId;
    }

    public void Deserialize(ref ReadOnlySpan<char> rawString)
    {
        MessageName = rawString.Trim().ToString();
    }
    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        var charSize = ULog.Encoding.GetCharCount(buffer);
        var charBuffer = new char[charSize];
        ULog.Encoding.GetChars(buffer,charBuffer);
        var rawString = new ReadOnlySpan<char>(charBuffer, 0, charSize);
        Deserialize(ref rawString);
    }

    public void Serialize(ref Span<byte> buffer)
    {
    }
    private void CheckName(string? name)
    {
        ULog.CheckMessageName(name);

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
    }
}