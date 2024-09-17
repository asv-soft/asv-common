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
public class ULogParameterMessageToken : KeyValueTokenBase
{
    public static ULogToken Token => ULogToken.Parameter;
    public const string TokenName = "Parameter";
    public const byte TokenId = (byte)'P';
    
    public override string Name => TokenName;
    public override ULogToken Type => Token;
    public override TokenPlaceFlags Section => TokenPlaceFlags.Definition | TokenPlaceFlags.Data;

    public override void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        base.Deserialize(ref buffer);
        if (Key.Type.BaseType != ULogType.Float && Key.Type.BaseType != ULogType.Int32)
        {
            throw new ULogException($"Parameter message value type must be {ULogType.Float} or {ULogType.Int32}");
        }
    }

    public override void Serialize(ref Span<byte> buffer)
    {
        ArgumentNullException.ThrowIfNull(Key);
        if (Key.Type.BaseType != ULogType.Float && Key.Type.BaseType != ULogType.Int32)
        {
            throw new ULogException($"Parameter message value type must be {ULogType.Float} or {ULogType.Int32}");
        }
        base.Serialize(ref buffer);
    }

    public override int GetByteSize()
    {
        ArgumentNullException.ThrowIfNull(Key);
        if (Key.Type.BaseType != ULogType.Float && Key.Type.BaseType != ULogType.Int32)
        {
            throw new ULogException($"Parameter message value type must be {ULogType.Float} or {ULogType.Int32}");
        }
        return base.GetByteSize();
    }
}

