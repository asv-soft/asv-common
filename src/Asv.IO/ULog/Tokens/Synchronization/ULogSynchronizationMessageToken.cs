using System;

namespace Asv.IO;

/// <summary>
/// 'S': Synchronization message.
/// 
/// Message so that a reader can recover from a corrupt message by searching for the next sync message.
/// </summary>
public class ULogSynchronizationMessageToken : IULogToken
{
    #region Static

    public static ULogToken Type => ULogToken.Synchronization;
    public const string Name = "Synchronization";
    public const byte TokenId = (byte)'S';

    #endregion
    
    public string TokenName => Name;
    public ULogToken TokenType => Type;
    public TokenPlaceFlags TokenSection => TokenPlaceFlags.Data;
    
    /// <summary>
    /// Magic byte sequence for synchronization
    /// </summary>
    public static byte[] SyncMagic { get; } = [0x2F, 0x73, 0x13, 0x20, 0x25, 0x0C, 0xBB, 0x12];
    
    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        for (var i = 0; i < SyncMagic.Length; i++)
        {
            if (buffer[i] != SyncMagic[i])
            {
                throw new ULogException($"Error to parse Sync message: SyncMagic[{i}] want{SyncMagic[i]}. Got {buffer[i]}");
            }
        }
        
        buffer = buffer[SyncMagic.Length..];
    }

    public void Serialize(ref Span<byte> buffer)
    {
        for (var i = 0; i < SyncMagic.Length; i++)
        {
            buffer[i] = SyncMagic[i];
        }
        buffer = buffer[SyncMagic.Length..];
    }

    public int GetByteSize() => SyncMagic.Length;
}