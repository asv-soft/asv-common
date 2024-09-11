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
public sealed class ULogParameterMessageToken : IULogToken
{
    public static ULogToken Token => ULogToken.Parameter;
    public const string TokenName = "Parameter";
    public const byte TokenId = (byte)'P';
    
    public string Name => TokenName;
    public ULogToken Type => Token;
    public TokenPlaceFlags Section => TokenPlaceFlags.Definition | TokenPlaceFlags.Data;

    /// <summary>
    /// Key of the Token
    ///
    /// Every key value pair must be unique
    /// </summary>
    public ParameterTokenKey Key { get; set; } = null!;
    
    /// <summary>
    /// Value of the token
    /// 
    /// Every key value pair must be unique
    /// The data type is restricted to int32_t and float
    /// </summary>
    public ParameterTokenValue Value { get; set; }

    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        var keyLen = BinSerialize.ReadByte(ref buffer);
        var key = buffer[..keyLen];
        buffer = buffer[keyLen..];
        
        Key = new ParameterTokenKey();
        Key.Deserialize(ref key);
        
        ArgumentNullException.ThrowIfNull(Key.Type);
        var value = buffer;
        
        Value = new ParameterTokenValue
        {
            RawValue = value.ToArray(),
            Type = Key.Type
        };
        
        buffer = buffer[value.Length..];
    }

    public void Serialize(ref Span<byte> buffer) // TODO: test Serealization
    {
        Key.Serialize(ref buffer);
        Value.RawValue.CopyTo(buffer);
    }

    public int GetByteSize()
    {
        return Key.GetByteSize() + Value.GetByteSize();
    }
}


/// <summary>
/// Class that describes Token's key
/// </summary>
public class ParameterTokenKey : ISizedSpanSerializable
{
    private ULogDataType _type;
    private string _name;
    
    /// <summary>
    /// Length of the key
    /// </summary>
    public byte Length { get; set; }
    
    /// <summary>
    /// Type of the key
    /// 
    /// The data type is restricted to int32_t and float
    /// </summary>
    public ULogDataType Type
    {
        get => _type;
        set
        {
            CheckType(value);
            _type = value;
        }
    }
    
    /// <summary>
    /// Name of the key
    /// 
    /// It must match regex: a-zA-Z0-9_-/
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            CheckName(value);
            _name = value;
        }
    }

    public void Deserialize(ref ReadOnlySpan<char> rawString)
    {
        var colonIndex = rawString.IndexOf(ULog.TypeAndNameSeparator);
        if (colonIndex == -1)
        {
            throw new ULogException($"Invalid format field: '{ULog.TypeAndNameSeparator}' not found. Origin string: {rawString.ToString()}");
        }
        
        var type = rawString[..colonIndex];
        Type = ULog.ParseDataType(ref type, out _, out _);
        Name = rawString[(colonIndex + 1)..].Trim().ToString();
        Length = (byte)(Name.Length + type.Length + ULog.TypeAndNameSeparator.Length); 
    }
    
    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        var charSize = ULog.Encoding.GetCharCount(buffer);
         
        var charBuffer = ArrayPool<char>.Shared.Rent(charSize);
        var rawString = new ReadOnlySpan<char>(charBuffer, 0, charSize);
        ULog.Encoding.GetChars(buffer,charBuffer);
        Deserialize(ref rawString);
    }

    public void Serialize(ref Span<byte> buffer) // TODO: fix
    {
        buffer[0] = Length;
        buffer.Slice(1);
        Name.CopyTo(ref buffer, ULog.Encoding);
        ULog.TypeAndNameSeparator.CopyTo(ref buffer, ULog.Encoding);
        ULog.GetDataTypeName(Type, null).CopyTo(ref buffer, ULog.Encoding);
    }

    public int GetByteSize()
    {
        return 1 + ULog.Encoding.GetByteCount(ULog.GetDataTypeName(Type, null)) + ULog.TypeAndNameSeparatorByteSize + ULog.Encoding.GetByteCount(Name);
    }
    
    private void CheckType(ULogDataType? type)
    {
        ArgumentNullException.ThrowIfNull(type);

        switch (type)
        {
            case ULogDataType.Float:
            case ULogDataType.Int32:
                break;
            default:
                throw new ULogException($"Invalid key value: '{type}'. Must be {ULog.Int32TypeName} or {ULog.FloatTypeName}");
        }
    }
    
    private void CheckName(string? name)
    {
        ULog.CheckMessageName(name);
        
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
    }
}

/// <summary>
/// Struct that describes Token's value
/// </summary>
public struct ParameterTokenValue
{
    /// <summary>
    /// Value in bytes
    /// </summary>
    public required byte[] RawValue { get; init; }

    /// <summary>
    /// Type of the value
    /// 
    /// The data type is restricted to int32_t and float
    /// </summary>
    public required ULogDataType Type { get; init; }

    public int GetByteSize()
    {
        return RawValue.Length + ULog.Encoding.GetByteCount(ULog.GetDataTypeName(Type, null));
    }
}