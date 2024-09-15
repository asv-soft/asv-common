using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Asv.IO;

/// <summary>
/// 'M': Multi Information Message
///
/// This message is used to log multiple data fields in a single message.
/// </summary>
public partial class ULogMultiInformationMessageToken : IULogToken
{
    private const ULogToken TokenType = ULogToken.MultiInformation;
    private const string TokenName = "MultiInformation";
    public const byte TokenId = (byte)'M';
    public TokenPlaceFlags Section => TokenPlaceFlags.DefinitionAndData;
    public string Name => TokenName;
    public ULogToken Type => TokenType;

    private byte _isContinued;
    private byte _keyLenght;
    private string _key;
    private string _value;

    /// <summary>
    /// is_continued can be used for split-up messages:
    /// if set to 1, it is part of the previous message with the same key.
    /// </summary>
    public byte IsContinued
    {
        get => _isContinued;
        set => _isContinued = value;
    }
    
    /// <summary>
    /// key_len: Length of the key value
    /// </summary>
    public byte KeyLenght
    {
        get => _keyLenght;
        set => _keyLenght = value;
    }

    /// <summary>
    /// key: Contains the key string in the formtype name, e.g. char[value_len] sys_toolchain_ver.
    /// Valid characters for the name: a-zA-Z0-9_-/.
    /// The type may be one of the basic types including arrays.
    /// </summary>
    public string Key
    {
        get => _key;
        set
        {
            CheckKey(value);
            _key = value;
        }
    }
    
    /// <summary>
    /// value: Contains the data corresponding to the key e.g. 9.4.0
    /// </summary>
    public string Value
    {
        get => _value;
        set
        {
            CheckValue(value);
            _value = value;
        }
    }

    /// <summary>
    /// Explicitly written type from the Key string
    /// </summary>
    public ULogDataType KeyType { get; set; }

    /// <summary>
    /// Explicitly written name from the Key string
    /// </summary>
    public string KeyName { get; set; }

    /// <summary>
    /// Explicitly written value lenght from the Key string
    /// </summary>
    public int ValueLenght { get; set; }
    
    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        var charSize = ULog.Encoding.GetCharCount(buffer);
        var charBuffer = new char[charSize];
        var input = new Span<char>(charBuffer);
        var size = ULog.Encoding.GetChars(buffer,charBuffer);
        Debug.Assert(charSize == size);

        IsContinued = (byte)input[0];
        KeyLenght = (byte)input[1];
        
        Key = input[2..(KeyLenght + 2)].ToString();
        Value = input[(KeyLenght + 2)..].ToString();
    }

    public void Serialize(ref Span<byte> buffer)
    {
        BinSerialize.WriteByte(ref buffer, IsContinued);
        BinSerialize.WriteByte(ref buffer, KeyLenght);

        BinSerialize.WriteString(ref buffer, Key.AsSpan());
        BinSerialize.WriteString(ref buffer, Value.AsSpan());
    }

    public int GetByteSize()
    {
        return sizeof(byte) + sizeof(byte) + ULog.Encoding.GetByteCount(Key) + ULog.Encoding.GetByteCount(Value);
    }
    
    private const string FixedDataPattern = @"[a-zA-Z0-9_\-\/]+";
    [GeneratedRegex(FixedDataPattern, RegexOptions.Compiled)]
    private static partial Regex GetDataRegex();
    private static readonly Regex DataRegex = GetDataRegex();

    private void CheckKey(ReadOnlySpan<char> key)
    {
        if (key.IsEmpty)
            throw new ULogException("ULog multi info key is empty.");
        
        var separator = key.IndexOf(ULog.TypeAndNameSeparator);
        if (separator == -1)
            throw new ULogException($"Invalid key: '{ULog.TypeAndNameSeparator}' not found. Origin string: {key.ToString()}");
        var type = key[..separator];
        var name = key[separator..];

        KeyType = ULog.ParseDataType(ref type, out var typeName, out var valueLenght);
        ValueLenght = valueLenght;
        
        KeyName = name.Trim().ToString();
        if (DataRegex.IsMatch(name) == false)
            throw new ULogException($"Invalid ULog data name. Should be {DataRegex}. Origin value: '{key.ToString()}'");
    }

    private void CheckValue(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            throw new ULogException("ULog multi info value is empty.");
        if (value.Length != ValueLenght)            
            throw new ULogException($"Invalid value lenght: '{value.Length}'. Value lenght specified in the key: '{ValueLenght}'. Origin string: {value.ToString()}");
    }
}