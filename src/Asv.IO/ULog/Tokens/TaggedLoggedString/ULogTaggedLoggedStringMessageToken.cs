using System;

namespace Asv.IO;

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
    /// log_level: same as in the Linux kernel
    /// </summary>
    public ULogLevel LogLevel { get; set; }
    
    /// <summary>
    /// tag: id representing source of logged message string. It could represent a process, thread or a class depending upon the system architecture.
    /// </summary>
    public ushort Tag { get; set; }
    
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
        LogLevel = (ULogLevel)level;
        
        var tag = BinSerialize.ReadUShort(ref buffer);
        Tag = tag;
        
        Timestamp = BinSerialize.ReadULong(ref buffer);
        
        var messageBytes = buffer[..buffer.Length];
        Message = ULog.Encoding.GetString(messageBytes);
    }

    public void Serialize(ref Span<byte> buffer)
    {
        BinSerialize.WriteByte(ref buffer, (byte)LogLevel);
        BinSerialize.WriteUShort(ref buffer, Tag);
        BinSerialize.WriteULong(ref buffer, Timestamp);
        Message.CopyTo(ref buffer, ULog.Encoding);
    }

    public int GetByteSize()
    {
        return sizeof(ULogLevel)/*LogLevel*/ 
               + sizeof(ushort)/*Tag*/ 
               + sizeof(ulong)/*Timestamp*/ 
               + ULog.Encoding.GetByteCount(Message);
    }

    public enum ULogLevel : byte
    {
        /// <summary>
        /// System is unusable
        /// </summary>
        Emerg = 0,
        
        /// <summary>
        /// Action must be taken immediately
        /// </summary>
        Alert = 1,
        
        /// <summary>
        /// Critical conditions
        /// </summary>
        Crit = 2,
        
        /// <summary>
        /// Error conditions
        /// </summary>
        Err = 3,
        
        /// <summary>
        /// Warning conditions
        /// </summary>
        Warning = 4,
        
        /// <summary>
        /// Normal but significant condition
        /// </summary>
        Notice = 5,
        
        /// <summary>
        /// Informational
        /// </summary>
        Info = 6,
        
        /// <summary>
        /// Debug-level messages
        /// </summary>
        Debug = 7
    }
}