using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test;

public class ULogMultiInfoTokenTests
{
    private readonly ITestOutputHelper _output;

    public ULogMultiInfoTokenTests(ITestOutputHelper output)
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

        var paramsDict = new Dictionary<InformationTokenKey, IList<InformationTokenValue>>();
        while (reader.TryRead(ref rdr, out var token))
        {
            if (token != null && token.Type != ULogToken.MultiInformation) continue;
            if (token is not ULogMultiInformationMessageToken param) continue;
            if (!paramsDict.TryGetValue(param.InformationMessage.Key, out var value))
            {
                value = new List<InformationTokenValue> { param.InformationMessage.Value };
                paramsDict[param.InformationMessage.Key] = value; 
                continue;
            }

            value.Add(param.InformationMessage.Value);
        }
        
        Assert.NotNull(paramsDict);
        Assert.NotEmpty(paramsDict);

        foreach (var param in paramsDict)
        {
            var sb = new StringBuilder();
            foreach (var v in param.Value)
            {
                sb.Append(ValueToString(v));
            }

            var str = sb.ToString();
            _output.WriteLine($"MULTI INFO: {param.Key.Type} {param.Key.Name} values: {str}");    
        }
    }
    private string ValueToString(InformationTokenValue value)
    {
        if (value.RawValue.All(i => i == 0))
        {
            return "0";
        }

        return value.Type switch
        {
            ULogDataType.UInt32 or ULogDataType.Int32 => BitConverter.ToInt32(value.RawValue)
                .ToString(CultureInfo.InvariantCulture),
            ULogDataType.Char => CharToString(value).ToString(),
            _ => throw new ArgumentNullException("Wrong ulog value type for MultiInformationTokenValue")
        };
    }

    private ReadOnlySpan<char> CharToString(InformationTokenValue value)
    {
        var charSize = ULog.Encoding.GetCharCount(value.RawValue);
        var charBuffer = new char[charSize];
        ULog.Encoding.GetChars(value.RawValue,charBuffer);
        var rawString = new ReadOnlySpan<char>(charBuffer, 0, charSize);
        return rawString.ToString();

    }
    
    # region Deserialize
    [Theory]
    [InlineData(0, ULog.UInt32TypeName, "data", 24)]
    [InlineData(1, ULog.Int32TypeName, "data", 12)]
    [InlineData(1, ULog.CharTypeName, "data", 'd')]
    public void Multi_DeserializeToken_Success(byte isContinued, string type, string name, ValueType value)
    {
        var readOnlySpan = SetUpTestData(isContinued, type, name, value);
        var token = new ULogMultiInformationMessageToken();
        token.Deserialize(ref readOnlySpan);
        Assert.Equal(type, ULog.GetDataTypeName(token.InformationMessage.Key.Type, null));
        Assert.Equal(name, token.InformationMessage.Key.Name);
        Assert.Equal(value, InformationTokenValueToValueType(token.InformationMessage.Value));
    }

    [Theory]
    [InlineData(0, ULog.Int32TypeName, "%@#", 523)]
    [InlineData(1, ULog.CharTypeName, "`!!!`````````", 'd')]
    [InlineData(1, ULog.UInt32TypeName, "", 523)]
    [InlineData(1, ULog.Int32TypeName, null, 532)]
    public void Multi_DeserializeToken_WrongKeyName(byte isContinued, string type, string name, ValueType value)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(isContinued, type, name, value);
            var token = new ULogMultiInformationMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }

    [Theory]
    [InlineData(0, ULog.CharTypeName, "data", 12f)]
    [InlineData(1, ULog.Int32TypeName, "data", 3535)]
    [InlineData(0, ULog.UInt32TypeName, "data", 3535)]
    public void Multi_DeserializeToken_NoKeyBytes(byte isContinued, string type, string name, ValueType value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var readOnlySpan = SetUpTestDataWithoutKeyLength(isContinued, type, name, value);
            var token = new ULogMultiInformationMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }

    [Theory]
    [InlineData(0, ULog.Int32TypeName, "data", 123)]
    [InlineData(1, ULog.UInt32TypeName, "data", 321)]
    [InlineData(0, ULog.CharTypeName, "data", 'd')]
    public void Multi_DeserializeToken_WrongKeyBytes(byte isContinued, string type, string name, ValueType value)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(isContinued, type, name, value, 0);
            var token = new ULogMultiInformationMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }
    
    [Theory]
    [InlineData(0, null, "data", 'd')]
    [InlineData(1, null, "data", 1234)]
    [InlineData(1, null, "data", 12345)]
    public void Multi_DeserializeToken_NoType(byte isContinued, string type, string name, ValueType value)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(isContinued, type, name, value, 0);
            var token = new ULogMultiInformationMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }

    private ReadOnlySpan<byte> SetUpTestDataWithoutKeyLength(byte isContinued, string type, string name, ValueType value)
    {
        var key = type + ULog.TypeAndNameSeparator + name;
        var keyLength = (byte)key.Length;

        var keyBytes = ULog.Encoding.GetBytes(key);

        var valueBytes = value switch
        {
            char charValue => BitConverter.GetBytes(charValue),
            int int32Value => BitConverter.GetBytes(int32Value),
            uint uint32Value => BitConverter.GetBytes(uint32Value),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        var buffer = new Span<byte>(new byte[1 + 1 + ULog.Encoding.GetByteCount(key) + valueBytes.Length])
        {
            [0] = isContinued
        };

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

    # endregion

    private ReadOnlySpan<byte> SetUpTestData(byte isContinued, string type, string name, object value, byte? kLength = null)
    {
        var key = type + ULog.TypeAndNameSeparator + name;
        var keyLength = kLength ?? (byte)key.Length;

        var keyBytes = ULog.Encoding.GetBytes(key);

        var valueBytes = value switch
        {
            char charValue => BitConverter.GetBytes(charValue),
            int int32Value => BitConverter.GetBytes(int32Value),
            uint uint32Value => BitConverter.GetBytes(uint32Value),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        var buffer = new Span<byte>(new byte[1 + 1 + ULog.Encoding.GetByteCount(key) + valueBytes.Length])
        {
            [0] = isContinued,
            [1] = keyLength
        };

        for (var i = 0; i < keyBytes.Length; i++)
        {
            buffer[i + 2] = keyBytes[i];
        }

        for (var i = 0; i < valueBytes.Length; i++)
        {
            buffer[i + keyLength + 2] = valueBytes[i];
        }

        var byteArray = buffer.ToArray();
        var readOnlySpan = new ReadOnlySpan<byte>(byteArray);

        return readOnlySpan;
    }

    private static ValueType InformationTokenValueToValueType(InformationTokenValue value)
    {
        switch (value.Type)
        {
            case ULogDataType.UInt32:
            case ULogDataType.Int32:
                return BitConverter.ToInt32(value.RawValue);
            case ULogDataType.Char:
                var chars = BitConverter.ToChar(value.RawValue);
                return chars;
            default:
                throw new ArgumentNullException("Wrong ulog value type for InformationTokenValue");
        }
    }
}