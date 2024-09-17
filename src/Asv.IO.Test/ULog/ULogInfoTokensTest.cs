using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test;

public class ULogInfoTokensTest
{
    private readonly ITestOutputHelper _output;

    public ULogInfoTokensTest(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Fact]
    public void ReadHeaderWithParams()
    {
        var data = new ReadOnlySequence<byte>(TestData.ulog_log_small);
        var rdr = new SequenceReader<byte>(data); 
        var reader = ULog.CreateReader();
        var result = reader.TryRead<ULogFileHeaderToken>(ref rdr, out var header);
        Assert.True(result);
        Assert.NotNull(header);
        Assert.Equal(ULogToken.FileHeader,header.Type);
        Assert.Equal(20309082U, header.Timestamp);
        Assert.Equal(1,header.Version);
        result = reader.TryRead<ULogFlagBitsMessageToken>(ref rdr, out var flag);
        Assert.True(result);
        Assert.NotNull(flag);  
        Assert.Equal(ULogToken.FlagBits,flag.Type);

        var paramsDict = new Dictionary<string, IList<(ULogType,byte[])>>();
        while (reader.TryRead(ref rdr, out var token))
        {
            if (token.Type == ULogToken.Information)
            {
                if (token is ULogInformationMessageToken param)
                {
                    if (!paramsDict.ContainsKey(param.Key.Name))
                    {
                        paramsDict[param.Key.Name] = new List<(ULogType,byte[])> { new (param.Key.Type.BaseType,param.Value) }; 
                        continue;
                    }

                    paramsDict[param.Key.Name].Add(new (param.Key.Type.BaseType,param.Value));
                }
            }
        }
        
        Assert.NotNull(paramsDict);
        Assert.NotEmpty(paramsDict);

        foreach (var param in paramsDict)
        {
            var sb = new StringBuilder();
            foreach (var v in param.Value)
            {
                sb.Append(ValueToString(v.Item1,v.Item2));
            }

            var str = sb.ToString();
            _output.WriteLine($"{param.Key,-20} = {str}");    
        }
    }
    private string ValueToString(ULogType type, byte[] value)
    {
        switch (type)
        {
            case ULogType.UInt32:
            case ULogType.Int32:
                return BitConverter.ToInt32(value).ToString(CultureInfo.InvariantCulture);
            case ULogType.Char:
                 return CharToString(value).ToString();
            default:
                throw new ArgumentNullException("Wrong ulog value type for InformationTokenValue");
        }
    }

    private ReadOnlySpan<char> CharToString(byte[] value)
    {
        var charSize = ULog.Encoding.GetCharCount(value);
        var charBuffer = new char[charSize];
        ULog.Encoding.GetChars(value,charBuffer);
        var rawString = new ReadOnlySpan<char>(charBuffer, 0, charSize);
        return rawString.ToString();

    }
    
    # region Deserialize
    [Theory]
    [InlineData(ULogTypeDefinition.UInt32TypeName, "data", 24U)]
    [InlineData(ULogTypeDefinition.Int32TypeName, "data", 12)]
    [InlineData(ULogTypeDefinition.CharTypeName, "data", 'd')]
    public void DeserializeToken_Success(string type, string name, ValueType value)
    {
        var readOnlySpan = SetUpTestData(type, name, value);
        var token = new ULogInformationMessageToken();
        token.Deserialize(ref readOnlySpan);
        Assert.Equal(type, token.Key.Type.TypeName);
        Assert.Equal(name, token.Key.Name);
        Assert.Equal(value, InformationTokenValueToValueType(token.Key.Type.BaseType, token.Value));
    }

    [Theory]
    [InlineData(ULogTypeDefinition.Int32TypeName, "%@#", 523)]
    [InlineData(ULogTypeDefinition.CharTypeName, "`!!!`````````", 'd')]
    [InlineData(ULogTypeDefinition.UInt32TypeName, "", 523)]
    [InlineData(ULogTypeDefinition.Int32TypeName, null, 532)]
    public void DeserializeToken_WrongKeyName(string type, string name, ValueType value)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(type, name, value);
            var token = new ULogInformationMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }

    [Theory]
    [InlineData(ULogTypeDefinition.CharTypeName, "data", 12f)]
    [InlineData(ULogTypeDefinition.Int32TypeName, "data", 3535)]
    [InlineData(ULogTypeDefinition.UInt32TypeName, "data", 3535)]
    public void DeserializeToken_NoKeyBytes(string type, string name, ValueType value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var readOnlySpan = SetUpTestDataWithoutKeyLength(type, name, value);
            var token = new ULogInformationMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }

    [Theory]
    [InlineData(ULogTypeDefinition.Int32TypeName, "data", 123)]
    [InlineData(ULogTypeDefinition.UInt32TypeName, "data", 321)]
    [InlineData(ULogTypeDefinition.CharTypeName, "data", 'd')]
    public void DeserializeToken_WrongKeyBytes(string type, string name, ValueType value)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(type, name, value, 0);
            var token = new ULogInformationMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }
    
    [Theory]
    [InlineData(null, "data", 'd')]
    [InlineData(null, "data", 1234)]
    [InlineData(null, "data", 12345)]
    public void DeserializeToken_NoType(string type, string name, ValueType value)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(type, name, value, 0);
            var token = new ULogInformationMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }

    private ReadOnlySpan<byte> SetUpTestDataWithoutKeyLength(string type, string name, ValueType value)
    {
        var key = type + ULogTypeAndNameDefinition.TypeAndNameSeparator + name;
        var keyLength = (byte)key.Length;

        var keyBytes = ULog.Encoding.GetBytes(key);

        byte[] valueBytes = value switch
        {
            char charValue => BitConverter.GetBytes(charValue),
            Int32 int32Value => BitConverter.GetBytes(int32Value),
            UInt32 uint32Value => BitConverter.GetBytes(uint32Value),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        var buffer = new Span<byte>(new byte[1 + ULog.Encoding.GetByteCount(key) + valueBytes.Length]);

        for (var i = 0; i < keyBytes.Length; i++)
        {
            buffer[i] = keyBytes[i];
        }

        for (var i = 0; i < valueBytes.Length; i++)
        {
            buffer[i + keyLength] = valueBytes[i];
        }

        var byteArray = buffer.ToArray();
        var readOnlySpan = new ReadOnlySpan<byte>(byteArray);

        return readOnlySpan;
    }

    # endregion

    private ReadOnlySpan<byte> SetUpTestData(string type, string name, object value, byte? kLength = null)
    {
        var key = type + ULogTypeAndNameDefinition.TypeAndNameSeparator + name;
        var keyLength = kLength ?? (byte)key.Length;

        var keyBytes = ULog.Encoding.GetBytes(key);

        byte[] valueBytes = value switch
        {
            char charValue => BitConverter.GetBytes(charValue),
            Int32 int32Value => BitConverter.GetBytes(int32Value),
            UInt32 uint32Value => BitConverter.GetBytes(uint32Value),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        var buffer = new Span<byte>(new byte[1 + ULog.Encoding.GetByteCount(key) + valueBytes.Length]);
        buffer[0] = keyLength;

        for (var i = 0; i < keyBytes.Length; i++)
        {
            buffer[i + 1] = keyBytes[i];
        }

        for (var i = 0; i < valueBytes.Length; i++)
        {
            buffer[i + keyLength + 1] = valueBytes[i];
        }

        var byteArray = buffer.ToArray();
        var readOnlySpan = new ReadOnlySpan<byte>(byteArray);

        return readOnlySpan;
    }

    private ValueType InformationTokenValueToValueType(ULogType type, byte[] value)
    {
        switch (type)
        {
            case ULogType.Float:
                return BitConverter.ToSingle(value);
            case ULogType.Int32:
                return BitConverter.ToInt32(value);
            case ULogType.UInt32:
                return BitConverter.ToUInt32(value);
            case ULogType.Char:
                return BitConverter.ToChar(value);
            case ULogType.Int8:
                return (sbyte)value[0];
            case ULogType.UInt8:
                return value[0];
            case ULogType.Int16:
                return BitConverter.ToInt16(value);
            case ULogType.UInt16:
                return BitConverter.ToUInt16(value);
            case ULogType.Int64:
                return BitConverter.ToInt64(value);
            case ULogType.UInt64:
                return BitConverter.ToUInt64(value);
            case ULogType.Double:
                return BitConverter.ToDouble(value);
            case ULogType.Bool:
                return value[0] != 0;
            case ULogType.ReferenceType:
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
        
    }
}