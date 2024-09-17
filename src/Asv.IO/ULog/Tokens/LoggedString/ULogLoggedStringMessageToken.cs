using System;
using System.Buffers;

namespace Asv.IO;

/// <summary>
/// 'L': Logged String Message
/// 
/// Logged string message, i.e. printf() output.
/// </summary>
public class ULogLoggedStringMessageToken : IULogToken
{
    #region Static

    public static ULogToken Token => ULogToken.LoggedString;
    public const string Names = "Logged String message";
    public const byte TokenId = (byte)'L';
    private LogLevel _logLevel;
    
    #endregion
    
    private ulong _time;
    
    public string Name { get; }
    public ULogToken Type { get; }
    public TokenPlaceFlags Section { get; }
    public string TokenName { get; }
    public ULogToken TokenType { get; }
    public TokenPlaceFlags TokenSection { get; }
    
    /// <summary>
    /// log level same as in the Linux kernel:
    /// </summary>
    public LogLevel Level
    {
        get => _logLevel;
        set
        {
            CheckLevel(value);
            _logLevel = value;
        }
    }
    
    /// <summary>
    /// TimeStamp value in microseconds
    /// </summary>
    public DateTime Time { get; set; } = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    
    public string Message { get; set; }
    

    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        Level = (LogLevel)(char)BinSerialize.ReadByte(ref buffer);
        Time = Time.AddMicroseconds(BinSerialize.ReadULong(ref buffer));
        var charSize = ULog.Encoding.GetCharCount(buffer);
        var charBuffer = ArrayPool<char>.Shared.Rent(charSize);
        ULog.Encoding.GetChars(buffer, charBuffer);
        Message = new ReadOnlySpan<char>(charBuffer, 0, charSize).Trim().ToString();
        buffer = buffer[Message.Length..];
    }

    public void Serialize(ref Span<byte> buffer)
    {
        BinSerialize.WriteByte(ref buffer, (byte)Level);
        BinSerialize.WriteULong(ref buffer, (ulong)Time.Microsecond); 
        BinSerialize.WriteString(ref buffer, Message);
    }

    public int GetByteSize()
    {
        return sizeof(byte) + sizeof(ulong) + ULog.Encoding.GetByteCount(Message);
    }
    
    private void CheckLevel(LogLevel value)
    {
        if (!Enum.IsDefined(typeof(LogLevel), value))
        {
            throw new ULogException($"Invalid log level: {value}");
        }
    }
}

[Flags]
public enum LogLevel
{
    /// <summary>
    /// System is unusable
    /// </summary>
    Emerg = '0',
    
    /// <summary>
    /// Action must be taken immediately
    /// </summary>
    Alert = '1',
    
    /// <summary>
    /// Critical conditions
    /// </summary>
    Crit = '2',
    
    /// <summary>
    /// Error conditions
    /// </summary>
    Err = '3',
    
    /// <summary>
    /// Warning conditions
    /// </summary>
    Warning	= '4',
    
    /// <summary>
    /// Normal but significant condition
    /// </summary>
    Notice = '5',
    
    /// <summary>
    /// Informational
    /// </summary>
    Info = '6',
    
    /// <summary>
    /// Debug-level messages
    /// </summary>
    Debug = '7',
}