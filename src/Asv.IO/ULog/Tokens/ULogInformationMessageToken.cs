using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Asv.IO;

/// <summary>
/// 'I': Information Message
///
/// The Information message defines a dictionary type definition key : value pair for any information,
/// including but not limited to Hardware version, Software version, Build toolchain for the software, etc.
/// </summary>
public class ULogInformationMessageToken : IULogToken
{
    public static ULogToken Token => ULogToken.Information;
    public const string TokenName = "Information";
    public const byte TokenId = (byte)'I';

    public string Name => TokenName;
    public ULogToken Type => Token;
    public TokenPlaceFlags Section => TokenPlaceFlags.Definition| TokenPlaceFlags.Data;
    
    /// <summary>
    /// Key of the Token
    ///
    /// Every key value pair must be unique
    /// </summary>
    public InformationTokenKey Key { get; set; } = null!;
    /// <summary>
    /// Value of the token
    /// 
    /// Every key value pair must be unique
    /// </summary>
    public InformationTokenValue Value { get; set; }

    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        int keyLen = BinSerialize.ReadByte(ref buffer);
        var key = buffer[..keyLen];
        buffer = buffer[keyLen..];
        
        Key = new InformationTokenKey();
        Key.Deserialize(ref key);
        ArgumentNullException.ThrowIfNull(Key.Type);
        var value = buffer;
        Value = new InformationTokenValue
        {
            RawValue = value.ToArray(),
            Type = Key.Type
        };
        buffer = buffer[value.Length..];
    }

    public void Serialize(ref Span<byte> buffer)
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
public class InformationTokenKey : ISizedSpanSerializable
{
    private string _name;
    private ULogDataType _type;
    
    public int ArraySize { get; set; }
    public bool IsArray => ArraySize>0;
    public string? ReferenceName { get; set; }
    
    /// <summary>
    /// Type of the key
    /// </summary>
    public ULogDataType Type { get; set; }
    
    /// <summary>
    /// Length of the key
    /// </summary>
    public byte Length { get; set; }
    
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
        Type = ULog.ParseDataType(ref type, out _, out var arrayCount);
        Name = rawString[(colonIndex + 1)..].Trim().ToString();
        ArraySize = arrayCount;
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

    public void Serialize(ref Span<byte> buffer)
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
    
    private static void CheckName(string?  value)
    {
        ULog.CheckMessageName(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
    }
}

/// <summary>
/// Struct that describes Token's value
/// </summary>
public struct InformationTokenValue
{ 
    /// <summary>
    /// Value in bytes
    /// </summary>
    public required byte[] RawValue { get; init; }
    
    /// <summary>
    /// Type of the value
    /// </summary>
    public required ULogDataType Type { get; init; }
    public int GetByteSize()
    {
        return RawValue.Length + ULog.Encoding.GetByteCount(ULog.GetDataTypeName(Type, null));
    }
}