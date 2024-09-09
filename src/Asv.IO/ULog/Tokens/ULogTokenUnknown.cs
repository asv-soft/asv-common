using System;
using System.Buffers;

namespace Asv.IO;

public class ULogTokenUnknown : IULogToken
{
    public const ULogToken TokenType = ULogToken.Unknown;
    public const string TokenName =  "Unknown";
    
    private readonly ushort _byteSize;
    public ULogTokenUnknown(byte type, ushort byteSize)
    {
        _byteSize = byteSize;
        UnknownType = type;
    }

    public string Name => TokenName;
    public ULogToken Type => TokenType;

    public byte UnknownType { get; set; }
    public byte[] Data { get; set; }
  
    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        Data = buffer.ToArray();
    }

    public void Serialize(ref Span<byte> buffer)
    {
        throw new NotImplementedException();
    }
}