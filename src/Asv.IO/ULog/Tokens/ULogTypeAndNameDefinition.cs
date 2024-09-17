using System;
using System.Buffers;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Asv.IO;

/// <summary>
/// Represents a definition of a type and name in a ULog message.
///
/// e.g. type[array_length] field_name
/// </summary>
public partial class ULogTypeAndNameDefinition:ISizedSpanSerializable
{
    #region Static

    private const string FixedNamePattern = @"[a-zA-Z0-9_]*";
    [GeneratedRegex(FixedNamePattern, RegexOptions.Compiled)]
    private static partial Regex GetNameRegex();
    public static readonly Regex NameRegex = GetNameRegex();

    public static void CheckName(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            throw new ULogException("ULog variable name is empty.");
        }
        if (NameRegex.IsMatch(value) == false)
        {
            throw new ULogException($"Invalid ULog '{nameof(ULogTypeAndNameDefinition)}' definition. '{nameof(Name)}' should be {FixedNamePattern}. Origin value: '{value.ToString()}'");
        }
    }
    
    public const char TypeAndNameSeparator = ' ';
    public static readonly int TypeAndNameSeparatorByteSize;
    private string _name = null!;


    static ULogTypeAndNameDefinition()
    {
        var temp = TypeAndNameSeparator;
        TypeAndNameSeparatorByteSize = ULog.Encoding.GetByteCount(new ReadOnlySpan<char>(ref temp));
    }
    
    #endregion
    
    public ULogTypeDefinition Type { get; set; } = null!;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            CheckName(value);
        }
    }

    public int GetByteSize()
    {
        return ULog.Encoding.GetByteCount(Name) + TypeAndNameSeparatorByteSize + Type.GetByteSize();
    }

    public void Deserialize(ref ReadOnlySpan<char> rawString)
    {
        var colonIndex = rawString.IndexOf(TypeAndNameSeparator);
        if (colonIndex == -1)
        {
            throw new ULogException($"Invalid format field: '{TypeAndNameSeparator}' not found. Origin string: {rawString.ToString()}");
        }
        var type = rawString[..colonIndex];
        Type = new ULogTypeDefinition();
        Type.Deserialize(type);
        Name = rawString[(colonIndex + 1)..].Trim().ToString();
    }
    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        var charSize = ULog.Encoding.GetCharCount(buffer);
        var charBuffer = ArrayPool<char>.Shared.Rent(charSize);
        var rawString = new ReadOnlySpan<char>(charBuffer, 0, charSize);
        var cnt = ULog.Encoding.GetChars(buffer, charBuffer);
        Debug.Assert(cnt == charSize);
        Deserialize(ref rawString);
    }

    public void Serialize(ref Span<byte> buffer)
    {
        //we need to write e.g. type[array_length] field_name
        Type.Serialize(ref buffer);
        var temp = TypeAndNameSeparator;
        var write = ULog.Encoding.GetBytes(new ReadOnlySpan<char>(ref temp), buffer);
        buffer = buffer[write..];
        Name.CopyTo(ref buffer, ULog.Encoding);
        
    }
}