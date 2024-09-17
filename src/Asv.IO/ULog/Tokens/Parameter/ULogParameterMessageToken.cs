using System;
using System.Buffers;

namespace Asv.IO;

/// <summary>
/// 'P': Parameter Message
///
/// Parameter message in the Definitions section defines the parameter values of the vehicle when logging is started.
/// It uses the same format as the Information Message.
///
/// If a parameter dynamically changes during runtime, this message can also be used in the Data section as well.
/// </summary>
public sealed class ULogParameterMessageToken : IULogToken
{
    public static ULogToken Token => ULogToken.Parameter;
    public const string TokenName = "Parameter";
    public const byte TokenId = (byte)'P';
    
    public string Name => TokenName;
    public ULogToken Type => Token;
    public TokenPlaceFlags Section => TokenPlaceFlags.Definition | TokenPlaceFlags.Data;

    /// <summary>
    /// Key of the Token
    ///
    /// Every key value pair must be unique
    /// </summary>
    public ULogTypeAndNameDefinition Key { get; set; } = null!;
    
    /// <summary>
    /// Value of the token
    /// 
    ///  The data type is restricted to int32_t and float
    /// </summary>
    public byte[] Value { get; set; }

    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        var keyLen = BinSerialize.ReadByte(ref buffer);
        var key = buffer[..keyLen];
        Key = new ULogTypeAndNameDefinition();
        Key.Deserialize(ref key);
        if (Key.Type.BaseType != ULogType.Float && Key.Type.BaseType != ULogType.Int32)
        {
            throw new ULogException($"Parameter message value type must be {ULogType.Float} or {ULogType.Int32}");
        }
        buffer = buffer[keyLen..];
        Value = buffer.ToArray();
    }

    public void Serialize(ref Span<byte> buffer) // TODO: test Serealization
    {
        Key.Serialize(ref buffer);
        Value.CopyTo(buffer);
        buffer = buffer[Value.Length..];
    }

    public int GetByteSize()
    {
        return Key.GetByteSize() + Value.Length;
    }
}

