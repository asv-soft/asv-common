using System;

namespace Asv.IO;

/// <summary>
/// Base class for ULog key-value tokens.
///
/// e.g. Information, Parameter, etc.
/// 
/// uint8_t key_len;
/// char key[key_len];
/// char value[header.msg_size-2-key_len]
/// </summary>
public abstract class ULogKeyAndValueTokenBase: IULogToken
{
    public virtual void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        var keyLen = BinSerialize.ReadByte(ref buffer);
        var key = buffer[..keyLen];
        Key = new ULogTypeAndNameDefinition();
        Key.Deserialize(ref key);
        buffer = buffer[keyLen..];
        Value = buffer.ToArray();
    }

    public virtual void Serialize(ref Span<byte> buffer)
    {
        Key.Serialize(ref buffer);
        Value.CopyTo(buffer);
        buffer = buffer[Value.Length..];
    }

    public virtual int GetByteSize()
    {
        return Key.GetByteSize() + Value.Length;
    }

    public abstract string TokenName { get; }
    public abstract ULogToken TokenType { get; }
    public abstract TokenPlaceFlags TokenSection { get; }
    
    public ULogTypeAndNameDefinition Key { get; set; } = null!;
    public byte[] Value { get; set; }
}