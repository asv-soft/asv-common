using System;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Asv.IO;

public static partial class ULog
{
    public static readonly Encoding Encoding = Encoding.UTF8;
    
    public const string ArrayStart = "[";
    public static readonly int ArrayStartByteSize = Encoding.GetByteCount(ArrayStart);
    public const string ArrayEnd = "]";
    public static readonly int ArrayEndByteSize = Encoding.GetByteCount(ArrayEnd);
    public const string TypeAndNameSeparator = " ";
    public static readonly int TypeAndNameSeparatorByteSize = Encoding.GetByteCount(TypeAndNameSeparator);
    public const string FieldSeparator = ";";
    public static readonly int FieldSeparatorByteSize = Encoding.GetByteCount(FieldSeparator);
    public const string MessageAndFieldsSeparator = ":";
    public static readonly int MessageAndFieldsSeparatorByteSize = Encoding.GetByteCount(MessageAndFieldsSeparator);
    public const string Int8TypeName = "int8_t";
    public const string UInt8TypeName = "uint8_t";
    public const string Int16TypeName = "int16_t";
    public const string UInt16TypeName = "uint16_t";
    public const string Int32TypeName = "int32_t";
    public const string UInt32TypeName = "uint32_t";
    public const string Int64TypeName = "int64_t";
    public const string UInt64TypeName = "uint64_t";
    public const string FloatTypeName = "float";
    public const string DoubleTypeName = "double";
    public const string BoolTypeName = "bool";
    public const string CharTypeName = "char";
    

    private const string FixedMessageNamePattern = @"[a-zA-Z0-9_\-\/]+";
    [GeneratedRegex(FixedMessageNamePattern, RegexOptions.Compiled)]
    private static partial Regex GetMessageNameRegex();

    public static readonly Regex MessageNameRegex = GetMessageNameRegex();

    private const string FixedFieldNamePattern = @"a-zA-Z0-9_";
    [GeneratedRegex(FixedFieldNamePattern, RegexOptions.Compiled)]
    private static partial Regex GetFieldNameRegex();
    public static readonly Regex FiledNameRegex = GetFieldNameRegex();
    public static IULogReader CreateReader(ILogger? logger = null)
    {
        var builder = ImmutableDictionary.CreateBuilder<byte, Func<IULogToken>>();
        builder.Add(ULogFlagBitsMessageToken.TokenId, () => new ULogFlagBitsMessageToken());
        builder.Add(ULogFormatMessageToken.TokenId, () => new ULogFormatMessageToken());
        builder.Add(ULogParameterMessageToken.TokenId, () => new ULogParameterMessageToken()); //TODO delete before pr
        return new ULogReader(builder.ToImmutable(),logger);
    }


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
    
    public static void CheckFieldName(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            throw new ULogException("ULog field name is empty.");
        }
        if (MessageNameRegex.IsMatch(value) == false)
        {
            throw new ULogException($"Invalid ULog field name. Should be {FixedMessageNamePattern}. Origin value: '{value.ToString()}'");
        }
    }

    public static ULogDataType ParseDataType(ref ReadOnlySpan<char> data, out ReadOnlySpan<char> referenceTypeName, out int arrayCount)
    {
        arrayCount = 0;
        // Trim any leading or trailing whitespace
        data = data.Trim();

        // Check for array format (e.g., float[5])
        var openBracketIndex = data.IndexOf(ArrayStart);
        var closeBracketIndex = data.IndexOf(ArrayEnd);

        if (openBracketIndex != -1 && closeBracketIndex != -1)
        {
            // Parse array size
            var arraySizeSpan = data.Slice(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1);
            if (!int.TryParse(arraySizeSpan, out arrayCount))
            {
                throw new FormatException("Invalid array size format.");
            }

            // Extract the type name without the array size
            referenceTypeName = data[..openBracketIndex].Trim();
        }
        else
        {
            // If not an array, use the entire string as the type name
            referenceTypeName = data;
        }

        // Identify the type based on its string representation
        if (referenceTypeName.SequenceEqual(Int8TypeName.AsSpan())) return ULogDataType.Int8;
        if (referenceTypeName.SequenceEqual(UInt8TypeName.AsSpan())) return ULogDataType.UInt8;
        if (referenceTypeName.SequenceEqual(Int16TypeName.AsSpan())) return ULogDataType.Int16;
        if (referenceTypeName.SequenceEqual(UInt16TypeName.AsSpan())) return ULogDataType.UInt16;
        if (referenceTypeName.SequenceEqual(Int32TypeName.AsSpan())) return ULogDataType.Int32;
        if (referenceTypeName.SequenceEqual(UInt32TypeName.AsSpan())) return ULogDataType.UInt32;
        if (referenceTypeName.SequenceEqual(Int64TypeName.AsSpan())) return ULogDataType.Int64;
        if (referenceTypeName.SequenceEqual(UInt64TypeName.AsSpan())) return ULogDataType.UInt64;
        if (referenceTypeName.SequenceEqual(FloatTypeName.AsSpan())) return ULogDataType.Float;
        if (referenceTypeName.SequenceEqual(DoubleTypeName.AsSpan())) return ULogDataType.Double;
        if (referenceTypeName.SequenceEqual(BoolTypeName.AsSpan())) return ULogDataType.Bool;
        if (referenceTypeName.SequenceEqual(CharTypeName.AsSpan())) return ULogDataType.Char;

        // Return ReferenceType if the type is not recognized
        return ULogDataType.ReferenceType;
    }


    public static string GetDataTypeName(ULogDataType fieldType, string? referenceName)
    {
        return fieldType switch
        {
            ULogDataType.Int8 => Int8TypeName,
            ULogDataType.UInt8 => UInt8TypeName,
            ULogDataType.Int16 => Int16TypeName,
            ULogDataType.UInt16 => UInt16TypeName,
            ULogDataType.Int32 => Int32TypeName,
            ULogDataType.UInt32 => UInt32TypeName,
            ULogDataType.Int64 => Int64TypeName,
            ULogDataType.UInt64 => UInt64TypeName,
            ULogDataType.Float => FloatTypeName,
            ULogDataType.Double => DoubleTypeName,
            ULogDataType.Bool => BoolTypeName,
            ULogDataType.Char => CharTypeName,
            ULogDataType.ReferenceType => referenceName ?? throw new ArgumentNullException(nameof(referenceName)),
            _ => throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, null)
        };
    }
    
    
}

public enum ULogDataType
{
    Int8,
    UInt8,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Float,
    Double,
    Bool,
    Char,
    ReferenceType
}