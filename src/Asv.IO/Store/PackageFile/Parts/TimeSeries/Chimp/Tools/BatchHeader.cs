using System;
using System.IO;

namespace Asv.IO;

public class BatchHeader : ISizedSpanSerializable
{
    public const byte StartSignatureString = (byte)'[';
    public const byte EndSignatureString = (byte)']';

    public string? Name { get; set; }
    public uint FieldCount { get; set; }
    public uint RawCount { get; set; }

    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        var start = BinSerialize.ReadByte(ref buffer);
        if (start != StartSignatureString)
        {
            throw new InvalidOperationException("Invalid start signature");
        }

        Name = BinSerialize.ReadString(ref buffer);
        FieldCount = BinSerialize.ReadPackedUnsignedInteger(ref buffer);
        RawCount = BinSerialize.ReadPackedUnsignedInteger(ref buffer);
        var end = BinSerialize.ReadByte(ref buffer);
        if (end != EndSignatureString)
        {
            throw new InvalidOperationException("Invalid end signature");
        }
    }

    public void Serialize(ref Span<byte> buffer)
    {
        BinSerialize.WriteByte(ref buffer, StartSignatureString);
        BinSerialize.WriteString(ref buffer, Name);
        BinSerialize.WritePackedUnsignedInteger(ref buffer, FieldCount);
        BinSerialize.WritePackedUnsignedInteger(ref buffer, RawCount);
        BinSerialize.WriteByte(ref buffer, EndSignatureString);
    }

    public int GetByteSize()
    {
        return BinSerialize.GetSizeForString(Name)
            + BinSerialize.GetSizeForPackedUnsignedInteger(FieldCount)
            + BinSerialize.GetSizeForPackedUnsignedInteger(RawCount)
            + (2 * sizeof(byte));
    }

    public void ReadFrom(Stream stream)
    {
        var start = stream.ReadByte();
        if (start < 0)
        {
            throw new EndOfStreamException();
        }

        if (start != StartSignatureString)
        {
            throw new InvalidOperationException("Invalid start signature");
        }

        Name = BinSerialize.ReadString(stream);
        FieldCount = BinSerialize.ReadPackedUnsignedInteger(stream);
        RawCount = BinSerialize.ReadPackedUnsignedInteger(stream);
        var end = stream.ReadByte();
        if (end != EndSignatureString)
        {
            throw new InvalidOperationException("Invalid end signature");
        }
    }
}
