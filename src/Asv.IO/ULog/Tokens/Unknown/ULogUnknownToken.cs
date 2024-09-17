using System;
using System.Buffers;
using System.Text;

namespace Asv.IO;

public class ULogUnknownToken : IULogToken
{
    #region Static

    public const ULogToken Type = ULogToken.Unknown;
    public const string Name =  "Unknown";

    #endregion
    
    private readonly ushort _byteSize;
    private byte _unknownType;

    public ULogUnknownToken(byte type, ushort byteSize)
    {
        _byteSize = byteSize;
        UnknownType = type;
    }

    public string TokenName => Name;
    public ULogToken TokenType => Type;
    public TokenPlaceFlags TokenSection => TokenPlaceFlags.DefinitionAndData;

    public char UnknownTypeChar { get; private set; }

    public byte UnknownType
    {
        get => _unknownType;
        set
        {
            _unknownType = value;
            var buff = new char[1];
            ULog.Encoding.GetChars(new ReadOnlySpan<byte>([_unknownType]), new Span<char>(buff));
            UnknownTypeChar = buff[0];
        }
    }

    public byte[] Data { get; set; }
  
    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        Data = buffer.ToArray();
    }

    public void Serialize(ref Span<byte> buffer)
    {
        Data.CopyTo(buffer);
        buffer = buffer[Data.Length..];
    }

    public int GetByteSize()
    {
        return _byteSize;
    }
}