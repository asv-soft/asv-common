using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

namespace Asv.IO;

/// <summary>
/// Header Section
/// The header is a fixed-size section and has the following format (16 bytes):
/// ----------------------------------------------------------------------
/// | 0x55 0x4c 0x6f 0x67 0x01 0x12 0x35 | 0x01         | uint64_t       |
/// | File magic (7B)                    | Version (1B) | Timestamp (8B) |
/// ----------------------------------------------------------------------
/// </summary>
public class ULogTokenFileHeader: IULogToken
{
    /// <summary>
    /// File type indicator that reads "ULogXYZ where XYZ is the magic bytes sequence 0x01 0x12 0x35"
    /// </summary>
    private static readonly byte[] FileMagic = [0x55, 0x4c, 0x6f, 0x67, 0x01, 0x12, 0x35];

    private byte _version;
    private ulong _timestamp;
    public const string TokenName = "FileHeader";
    public const ULogToken TokenType = ULogToken.FileHeader;

    /// <summary>
    /// File format version
    /// </summary>
    public byte Version
    {
        get => _version;
        set => _version = value;
    }

    /// <summary>
    /// uint64_t integer that denotes when the logging started in microseconds.
    /// </summary>
    public ulong Timestamp
    {
        get => _timestamp;
        set => _timestamp = value;
    }

    public string Name => TokenName;
    public ULogToken Type => TokenType;

    public bool TryRead(ReadOnlySequence<byte> data)
    {
        var rdr = new SequenceReader<byte>(data);
        for (var i = 0; i < FileMagic.Length; i++)
        {
            if (rdr.TryRead(out var b) == false)
            {
                rdr.Rewind(i);
                return false;
            }
            if (b != FileMagic[i])
            {
                throw new ULogException($"Error to parse ULog header: FileMagic[{i}] want{FileMagic[i]}. Got {b}");
            }
        }

        if (rdr.TryRead(out _version) == false)
        {
            rdr.Rewind(FileMagic.Length);
            return false;
        }

        if (rdr.TryReadLittleEndian(out _timestamp) == false)
        {
            rdr.Rewind(FileMagic.Length + sizeof(byte)); // move back 7 (FileMagic) + 1 (version) bytes
            return false;
        }
        return true;
    }

    public void WriteTo(IBufferWriter<byte> writer)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"Version:{Version},Timestamp:{Timestamp}";
    }
}