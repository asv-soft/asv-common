using System;

namespace Asv.IO;

public abstract class KeyValueTokenBase: IULogToken
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
        BinSerialize.WriteByte(ref buffer, (byte) (Key.Type.TypeName.Length + sizeof(byte) + Key.Name.Length));
        Key.Serialize(ref buffer);
        Value.CopyTo(buffer);
        buffer = buffer[Value.Length..];
    }

    public virtual int GetByteSize()
    {
        return sizeof(byte) + Key.GetByteSize() + Value.Length;
    }

    public abstract string Name { get; }
    public abstract ULogToken Type { get; }
    public abstract TokenPlaceFlags Section { get; }
    
    public ULogTypeAndNameDefinition Key { get; set; } = null!;
    public byte[] Value { get; set; }
}