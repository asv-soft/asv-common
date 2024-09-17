using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Asv.IO;

/// <summary>
/// 'F': Format Message
///
/// Format message defines a single message name and its inner fields in a single string.
/// </summary>
public partial class ULogFormatMessageToken: IULogToken
{
    #region Static  
    
    public const char FieldSeparator = ';';
    public static readonly int FieldSeparatorByteSize;
    public const char MessageAndFieldsSeparator = ':';
    public static readonly int MessageAndFieldsSeparatorByteSize;

    private const string FixedMessageNamePattern = @"[a-zA-Z0-9_\-\/]+";
    [GeneratedRegex(FixedMessageNamePattern, RegexOptions.Compiled)]
    private static partial Regex GetMessageNameRegex();

    public static readonly Regex MessageNameRegex = GetMessageNameRegex();
    
    public static void CheckMessageName(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            throw new ULogException("ULog message name is empty.");
        }
        if (MessageNameRegex.IsMatch(value) == false)
        {
            throw new ULogException($"Invalid ULog message name. Should be {FixedMessageNamePattern}. Origin value: '{value.ToString()}'");
        }
    }
    
    static ULogFormatMessageToken()
    {
        var temp = FieldSeparator;
        FieldSeparatorByteSize = ULog.Encoding.GetByteCount(new ReadOnlySpan<char>(ref temp));
        temp = MessageAndFieldsSeparator;
        MessageAndFieldsSeparatorByteSize = ULog.Encoding.GetByteCount(new ReadOnlySpan<char>(ref temp));
    }
    
    #endregion
    
    public static ULogToken Token => ULogToken.Format;
    public const string TokenName = "Format";
    public const byte TokenId = (byte)'F';

    
    
    
    public string Name => TokenName;
    public ULogToken Type => Token;
    public TokenPlaceFlags Section => TokenPlaceFlags.Definition;
    
    private string _messageName = null!;

    /// <summary>
    /// Message name
    /// </summary>
    public string MessageName
    {
        get => _messageName;
        set
        {
            CheckMessageName(value);
            _messageName = value;
        }
    }

    /// <summary>
    /// Message fields
    /// </summary>
    public IList<ULogTypeAndNameDefinition> Fields { get; } = [];

    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        var charSize = ULog.Encoding.GetCharCount(buffer);
        var charBuffer = new char[charSize];
        var input = new ReadOnlySpan<char>(charBuffer);
        var size = ULog.Encoding.GetChars(buffer,charBuffer);
        Debug.Assert(charSize == size);
        
        var colonIndex = input.IndexOf(MessageAndFieldsSeparator);
        if (colonIndex == -1)
        {
            throw new ULogException($"Invalid format message for token {Token:G}: '{MessageAndFieldsSeparator}' not found. Origin string: {input.ToString()}");
        }
        var messageNameSpan = input[..colonIndex];
        MessageName = messageNameSpan.ToString();
        var fieldsSpan = input[(colonIndex + 1)..];
        while (!fieldsSpan.IsEmpty)
        {
            var semicolonIndex = fieldsSpan.IndexOf(FieldSeparator);
            if (semicolonIndex == -1)
            {
                throw new ULogException($"Invalid format message for token {Token:G}: '{FieldSeparator}' not found. Origin string: {fieldsSpan.ToString()}");
            }

            var field = fieldsSpan[..semicolonIndex];
            var newItem = new ULogTypeAndNameDefinition();
            newItem.Deserialize(ref field);
            Fields.Add(newItem);
            fieldsSpan = fieldsSpan[(semicolonIndex + 1)..];
        }

    }

    public void Serialize(ref Span<byte> buffer)
    {
        // we need to serialize message name and fields e.g. message_name:field1;field2;field3;
        MessageName.CopyTo(ref buffer, ULog.Encoding);
        var temp = MessageAndFieldsSeparator;
        var write = ULog.Encoding.GetBytes(new ReadOnlySpan<char>(ref temp), buffer);
        buffer = buffer[write..];
        foreach (var field in Fields)
        {
            field.Serialize(ref buffer);
            temp = FieldSeparator;
            write = ULog.Encoding.GetBytes(new ReadOnlySpan<char>(ref temp), buffer);
            buffer = buffer[write..];
        }
        
    }

    public int GetByteSize()
    {
        return ULog.Encoding.GetByteCount(MessageName) + MessageAndFieldsSeparatorByteSize + Fields.Sum(x => x.GetByteSize() + FieldSeparatorByteSize);
    }
}