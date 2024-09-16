using System;
using System.Security.AccessControl;

namespace Asv.IO;

/// <summary>
/// 'D': Logged Data Message 
/// </summary>
public class ULogLoggedDataMessageToken : IULogToken
{
    public static ULogToken Token => ULogToken.LoggedData;
    public const string TokenName = "LoggedData";
    public const byte TokenId = (byte)'D';

    public string Name => TokenName;
    public ULogToken Type => Token;
    public TokenPlaceFlags Section => TokenPlaceFlags.Data;

    /// <summary>
    /// msg_id: unique id to match Logged data Message data. The first use must set this to 0, then increase it.
    /// 
    /// The same msg_id must not be used twice for different subscriptions.
    /// </summary>
    public ushort MessageId { get; set; }
    public byte Data { get; set; }
    
    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        throw new NotImplementedException();
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