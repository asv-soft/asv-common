using System;
using System.IO;

namespace Asv.IO;

public class FieldHeader : ISizedSpanSerializable
{
    public const byte StartField = (byte)'\t';
    public const byte EndOfField = (byte)'\n';
    public int Size { get; set; }
    public bool IsCompressed { get; set; }

    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        var start = BinSerialize.ReadByte(ref buffer);
        if (start != StartField)
        {
            throw new InvalidOperationException("Invalid start field");
        }

        Size = BinSerialize.ReadPackedInteger(ref buffer);
        IsCompressed = BinSerialize.ReadBool(ref buffer);
        var end = BinSerialize.ReadByte(ref buffer);
        if (end != EndOfField)
        {
            throw new InvalidOperationException("Invalid end field");
        }
    }

    public void Serialize(ref Span<byte> buffer)
    {
        BinSerialize.WriteByte(ref buffer, StartField);
        BinSerialize.WritePackedInteger(ref buffer, Size);
        BinSerialize.WriteBool(ref buffer, IsCompressed);
        BinSerialize.WriteByte(ref buffer, EndOfField);
    }

    public void ReadFrom(Stream stream)
    {
        var start = stream.ReadByte();
        if (start != StartField)
        {
            throw new InvalidOperationException("Invalid start field");
        }

        Size = BinSerialize.ReadPackedInteger(stream);
        IsCompressed = BinSerialize.ReadBool(stream);
        var end = stream.ReadByte();
        if (end != EndOfField)
        {
            throw new InvalidOperationException("Invalid end field");
        }
    }

    public int GetByteSize()
    {
        return BinSerialize.GetSizeForPackedInteger(Size) + sizeof(bool) + (2 * sizeof(byte));
    }
}
