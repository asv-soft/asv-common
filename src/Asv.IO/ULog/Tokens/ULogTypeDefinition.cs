using System;
using System.Buffers;

namespace Asv.IO;

public enum ULogType
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

/// <summary>
/// Represents a ULog type definition.
///
/// e.g., float[5], int8_t, etc.
/// </summary>
public class ULogTypeDefinition:ISizedSpanSerializable
{
    #region Static

    public const char ArrayStart = '[';
    public static readonly int ArrayStartByteSize;
    public const char ArrayEnd = ']';
    public static readonly int ArrayEndByteSize;
    
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
    
    static ULogTypeDefinition()
    {
        var temp = ArrayStart;
        ArrayStartByteSize = ULog.Encoding.GetByteCount(new ReadOnlySpan<char>(ref temp));
        temp = ArrayEnd;
        ArrayEndByteSize = ULog.Encoding.GetByteCount(new ReadOnlySpan<char>(ref temp));
    }

    #endregion
    
    private int _arraySize;
    private string _typeName = null!;
    private ULogType _baseType;

    public string TypeName
    {
        get => _typeName;
        set => _typeName = value;
    }

    public ULogType BaseType
    {
        get => _baseType;
        set => _baseType = value;
    }

    public bool IsArray => _arraySize > 0;
    
    public int ArraySize
    {
        get => _arraySize;
        set => _arraySize = value;
    }
    
    public void Deserialize(ReadOnlySpan<char> buffer)
    {
        _arraySize = 0;
        // Trim any leading or trailing whitespace
        buffer = buffer.Trim();
        // Check for array format (e.g., float[5])
        var openBracketIndex = buffer.IndexOf(ArrayStart);
        var closeBracketIndex = buffer.IndexOf(ArrayEnd);
        if (openBracketIndex != -1 && closeBracketIndex != -1)
        {
            // Parse array size
            var arraySizeSpan = buffer.Slice(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1);
            if (!int.TryParse(arraySizeSpan, out _arraySize))
            {
                throw new FormatException("Invalid array size format.");
            }

            // Extract the type name without the array size
            _typeName = buffer[..openBracketIndex].Trim().ToString();
        }
        else
        {
            // If not an array, use the entire string as the type name
            _typeName = buffer.Trim().ToString();
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(_typeName);
        _baseType = _typeName switch
        {
            Int8TypeName => ULogType.Int8,
            UInt8TypeName => ULogType.UInt8,
            Int16TypeName => ULogType.Int16,
            UInt16TypeName => ULogType.UInt16,
            Int32TypeName => ULogType.Int32,
            UInt32TypeName => ULogType.UInt32,
            Int64TypeName => ULogType.Int64,
            UInt64TypeName => ULogType.UInt64,
            FloatTypeName => ULogType.Float,
            DoubleTypeName => ULogType.Double,
            BoolTypeName => ULogType.Bool,
            CharTypeName => ULogType.Char,
            _ => ULogType.ReferenceType
        };
    }

    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        var charSize = ULog.Encoding.GetCharCount(buffer);
        var charBuffer = ArrayPool<char>.Shared.Rent(charSize);
        try
        {
            var rawString = new ReadOnlySpan<char>(charBuffer, 0, charSize);
            ULog.Encoding.GetChars(buffer, charBuffer);
            Deserialize(rawString);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(charBuffer);
        }
        
    }

    public void Serialize(ref Span<byte> buffer)
    {
        if (IsArray)
        {
            _typeName.CopyTo(ref buffer, ULog.Encoding);
            var temp = ArrayStart;
            var writed = ULog.Encoding.GetBytes(new ReadOnlySpan<char>(ref temp), buffer);
            buffer = buffer[writed..];
            _arraySize.ToString().CopyTo(ref buffer, ULog.Encoding);
            temp = ArrayEnd;
            writed = ULog.Encoding.GetBytes(new ReadOnlySpan<char>(ref temp), buffer);
        }
        else
        {
            _typeName.CopyTo(ref buffer, ULog.Encoding);
        }
    }

    public int GetByteSize()
    {
        return IsArray
            ? ULog.Encoding.GetByteCount(_typeName) +
              ArrayStartByteSize + ULog.Encoding.GetByteCount(_arraySize.ToString()) + ArrayEndByteSize
            : ULog.Encoding.GetByteCount(_typeName);
    }
}
