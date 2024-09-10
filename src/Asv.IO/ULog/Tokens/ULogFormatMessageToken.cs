using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Asv.IO;

/// <summary>
/// 'F': Format Message
///
/// Format message defines a single message name and its inner fields in a single string.
/// </summary>
public class ULogFormatMessageToken: IULogToken
{
    
    public static ULogToken Token => ULogToken.Format;
    public const string TokenName = "Format";
    public const byte TokenId = (byte)'F';

    
    
    private string? _messageName;
    public string Name => TokenName;
    public ULogToken Type => Token;

    public string? MessageName
    {
        get => _messageName;
        set
        {
            ULog.CheckMessageName(value);
            _messageName = value;
        }
    }
    
    public IList<FormatMessageField> Fields { get; } = [];

    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        var charSize = ULog.Encoding.GetCharCount(buffer);
        var charBuffer = new char[charSize];
        var input = new Span<char>(charBuffer);
        var size = ULog.Encoding.GetChars(buffer,charBuffer);
        Debug.Assert(charSize == size);
        
        var colonIndex = input.IndexOf(ULog.MessageAndFieldsSeparator);
        if (colonIndex == -1)
        {
            throw new ULogException($"Invalid format message for token {Token:G}: '{ULog.MessageAndFieldsSeparator}' not found. Origin string: {input.ToString()}");
        }
        var messageNameSpan = input[..colonIndex];
        MessageName = messageNameSpan.ToString();
        var fieldsSpan = input[(colonIndex + 1)..];
        while (!fieldsSpan.IsEmpty)
        {
            var semicolonIndex = fieldsSpan.IndexOf(ULog.FieldSeparator);
            if (semicolonIndex == -1)
            {
                throw new ULogException($"Invalid format message for token {Token:G}: '{ULog.FieldSeparator}' not found. Origin string: {fieldsSpan.ToString()}");
            }

            var field = fieldsSpan[..semicolonIndex];
            Fields.Add(new FormatMessageField(field));
            fieldsSpan = fieldsSpan[(semicolonIndex + 1)..];
        }

    }

    public void Serialize(ref Span<byte> buffer)
    {
        throw new NotImplementedException();
    }
}

public class FormatMessageField
{
    public FormatMessageField(Span<char> rawString)
    {
        var colonIndex = rawString.IndexOf(ULog.TypeAndNameSeparator);
        if (colonIndex == -1)
        {
            throw new ULogException($"Invalid format field: '{ULog.TypeAndNameSeparator}' not found. Origin string: {rawString.ToString()}");
        }
        var type = rawString[..colonIndex];
        FieldName = rawString[(colonIndex + 1)..].Trim().ToString();
        FieldType = ULog.ParseDataType(ref type, out var typeName, out var arrayCount);
        
        if (FieldType == ULogDataType.ReferenceType)
        {
            ReferenceName = typeName.Trim().ToString();
        }
        ArraySize = arrayCount;
    }

    public int ArraySize { get; set; }
    public string? ReferenceName { get; set; }
    public ULogDataType FieldType { get; set; }
    public string FieldName { get; set; }

    public bool IsArray => ArraySize>0;

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(ULog.GetDataTypeName(FieldType, ReferenceName));
        if (IsArray)
        {
            sb.Append(ULog.ArrayStart);
            sb.Append(ArraySize);
            sb.Append(ULog.ArrayEnd);
        }

        sb.Append(ULog.TypeAndNameSeparator);
        sb.Append(FieldName);
        return sb.ToString();
    }
    
}