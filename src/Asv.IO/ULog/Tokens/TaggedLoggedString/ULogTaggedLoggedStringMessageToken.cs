using System;

namespace Asv.IO.TaggedLoggedString;

public class ULogTaggedLoggedStringMessageToken : IULogToken
{
    #region Static

    public static ULogToken Token => ULogToken.TaggedLoggedString;
    public static readonly string Name = "TaggedLoggedString";
    public static readonly byte TokenId = (byte)'C';

    #endregion
 
    public string TokenName => Name;
    public ULogToken TokenType => Token;
    public TokenPlaceFlags TokenSection => TokenPlaceFlags.Data;

    /// <summary>
    /// log_level: same as in the Linux kernel:<list type=""></list>
    /// EMERG - '0' - System is unusable<list type=""></list>
    /// ALERT - '1' - Action must be taken immediately<list type=""></list>
    /// CRIT - '2' - Critical conditions<list type=""></list>
    /// ERR - '3' - Error conditions<list type=""></list>
    /// WARNING - '4' - Warning conditions<list type=""></list>
    /// NOTICE - '5' - Normal but significant condition<list type=""></list>
    /// INFO - '6' - Informational<list type=""></list>
    /// DEBUG - '7' - Debug-level messages<list type=""></list>
    /// </summary>
    public byte LogLevel { get; set; }
    
    /// <summary>
    /// tag: id representing source of logged message string. It could represent a process, thread or a class depending upon the system architecture.
    /// For example, a reference implementation for an onboard computer running multiple processes to control different payloads, external disks, serial devices etc can encode these process identifiers using a uint16_t enum into the tag attribute of struct as follows:
    /// <code>enum class ulog_tag : uint16_t {
    /// unassigned,
    /// mavlink_handler,
    /// ppk_handler,
    /// camera_handler,
    /// ptp_handler,
    /// serial_handler,
    /// watchdog,
    /// io_service,
    /// cbuf,
    /// ulg
    ///};</code>
    /// </summary>
    public MessageTag Tag { get; set; }
    
    /// <summary>
    /// timestamp: in microseconds
    /// </summary>
    public ulong Timestamp { get; set; }
    
    /// <summary>
    /// Logged string message, i.e. printf() output.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        var level = BinSerialize.ReadByte(ref buffer);
        if (level > 7) throw new ULogException($"Invalid ULog level: {level}. Must be between 0 and 7.");
        LogLevel = level;
        
        var tag = BinSerialize.ReadUShort(ref buffer);
        if (tag > 9) throw new ULogException($"Invalid ULog tag: {tag}");
        Tag = (MessageTag)tag;
        
        Timestamp = BinSerialize.ReadULong(ref buffer);
        
        var messageBytes = buffer[..buffer.Length];
        Message = ULog.Encoding.GetString(messageBytes);
    }

    public void Serialize(ref Span<byte> buffer)
    {
        BinSerialize.WriteByte(ref buffer, LogLevel);
        BinSerialize.WriteUShort(ref buffer, (ushort)Tag);
        BinSerialize.WriteULong(ref buffer, Timestamp);
        Message.CopyTo(ref buffer, ULog.Encoding);
    }

    public int GetByteSize()
    {
        return 1 + 2 + 8 + ULog.Encoding.GetByteCount(Message);
    }

    [Flags]
    public enum MessageTag
    {
        Unassigned = 0,
        MavlinkHandler = 1,
        PpkHandler = 2,
        CameraHandler = 3,
        PtpHandler = 4,
        SerialHandler = 5,
        Watchdog = 6,
        IoService = 7,
        Cbuf = 8,
        Ulg = 9
    }
}