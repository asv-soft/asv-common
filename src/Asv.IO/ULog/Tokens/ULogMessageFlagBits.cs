using System.Buffers;

namespace Asv.IO;

public class ULogMessageFlagBits:IULogToken
{
    private byte _compatFlags;
    private byte _incompatFlags;
    public static ULogToken Token => ULogToken.FlagBits;
    public const string TokenName = "FlagBits";
        
    public const byte TokenId = (byte)'B';
    public string Name => TokenName;
    public ULogToken Type => Token;
    public bool TryRead(ReadOnlySequence<byte> data)
    {
        var rdr = new SequenceReader<byte>(data);
        if (rdr.TryRead(out _compatFlags) == false) return false;
        if (rdr.TryRead(out _incompatFlags) == false)
        {
            rdr.Rewind(sizeof(byte)); // move back 1 byte (compatFlags)
            return false;
        }
        if (rdr.TryRead(out var append1) == false)
        {
            rdr.Rewind(sizeof(byte) * 2); // move back 2 bytes (compatFlags + incompatFlags)
            return false;
        }
        if (rdr.TryRead(out var append2) == false)
        {
            rdr.Rewind(sizeof(byte) * 3); // move back 3 bytes (compatFlags + incompatFlags + append1)
            return false;
        }
        if (rdr.TryRead(out var append3) == false)
        {
            rdr.Rewind(sizeof(byte) * 4); // move back 4 bytes (compatFlags + incompatFlags + append1 + append2)
            return false;
        }
        AppendedOffsets = (ulong)append1 | ((ulong)append2 << 8) | ((ulong)append3 << 16);
        return true;
        
    }

    /// <summary>
    /// appended_offsets: File offset (0-based) for appended data. If no data is appended, all offsets must be zero. This can be used to reliably append data for logs that may stop in the middle of a message. For example, crash dumps.
    /// 
    /// A process appending data should do:
    /// 
    /// set the relevant incompat_flags bit
    /// set the first appended_offsets that is currently 0 to the length of the log file without the appended data, as that is where the new data will start
    /// append any type of messages that are valid for the Data section. 
    /// </summary>
    public ulong AppendedOffsets { get; set; }
    /// <summary>
    /// compat_flags: compatible flag bits
    /// 
    /// These flags indicate the presence of features in the log file that are compatible with any ULog parser.
    /// compat_flags[0]: DEFAULT_PARAMETERS (Bit 0): if set, the log contains default parameters message
    /// The rest of the bits are currently not defined and must be set to 0. These bits can be used for future ULog changes that are compatible with existing parsers.
    /// For example, adding a new message type can be indicated by defining a new bit in the standard, and existing parsers will ignore the new message type.
    /// It means parsers can just ignore the bits if one of the unknown bits is set. 
    /// </summary>
    public byte CompatFlags
    {
        get => _compatFlags;
        set => _compatFlags = value;
    }
    /// <summary>
    /// incompat_flags: incompatible flag bits.
    /// 
    /// incompat_flags[0]: DATA_APPENDED (Bit 0): if set, the log contains appended data and at least one of the appended_offsets is non-zero.
    /// The rest of the bits are currently not defined and must be set to 0. This can be used to introduce breaking changes that existing parsers cannot handle.
    /// For example, when an old ULog parser that didn't have the concept of DATA_APPENDED reads the newer ULog,
    /// it would stop parsing the log as the log will contain out-of-spec messages / concepts. If a parser finds any of these bits set that isn't specified, it must refuse to parse the log.
    /// </summary>
    public byte IncompatFlags
    {
        get => _incompatFlags;
        set => _incompatFlags = value;
    }
    
    public void WriteTo(IBufferWriter<byte> writer)
    {
        throw new System.NotImplementedException();
    }
}