using System;

namespace Asv.IO;

/// <summary>
/// 'Q': Default Parameter Message
///
/// The default parameter message defines the default value of a parameter for a given vehicle and setup.
/// </summary>
public class ULogDefaultParameterMessageToken : ULogParameterMessageToken
{
    public new static ULogToken Token => ULogToken.DefaultParameter;
    public new const string TokenName = "Default Parameter";
    public new const byte TokenId = (byte)'Q';
    
    public override string Name => TokenName;
    public override ULogToken Type => Token;
    public override TokenPlaceFlags Section => TokenPlaceFlags.Definition | TokenPlaceFlags.Data;

    private ULogParameterDefaultTypes _defaultTypes;

    /// <summary>
    /// default_types is a bitfield and defines to which group(s) the value belongs to.
    /// 
    /// At least one bit must be set:
    ///     1&lt;&lt;0: system-wide default
    ///     1&lt;&lt;1: default for the current configuration (e.g. an airframe)
    /// 
    /// A log may not contain default values for all parameters.
    /// In those cases the default is equal to the parameter value, and different default types are treated independently.
    /// </summary>
    public ULogParameterDefaultTypes DefaultType
    {
        get => _defaultTypes;
        set
        {
            CheckDefaultType(value);
            _defaultTypes = value;
        }
    }
    
    public override void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        DefaultType = (ULogParameterDefaultTypes) BinSerialize.ReadByte(ref buffer);
        base.Deserialize(ref buffer);
    }

    public override void Serialize(ref Span<byte> buffer)
    {
        BinSerialize.WriteByte(ref buffer, (byte) DefaultType);
        base.Serialize(ref buffer);
    }

    public override int GetByteSize()
    {
        return sizeof(byte) + base.GetByteSize();
    }

    private void CheckDefaultType(ULogParameterDefaultTypes defaultType)
    {
        if (defaultType != ULogParameterDefaultTypes.None)
        {
            return;
        }
        
        throw new ULogException("Default parameter type is None");
    }
}

[Flags]
public enum ULogParameterDefaultTypes : byte
{
    None = 0,
    SystemWide = 1 << 0,
    ForCurrentConfiguration = 1 << 1,
}