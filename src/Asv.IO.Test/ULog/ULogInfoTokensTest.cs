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

        var paramsDict = new Dictionary<InformationTokenKey, IList<InformationTokenValue>>();
        while (reader.TryRead(ref rdr, out var token))
        {
            if (token.Type == ULogToken.Information)
            {
                if (token is ULogInformationMessageToken param)
                {
                    if (!paramsDict.ContainsKey(param.Key))
                    {
                        paramsDict[param.Key] = new List<InformationTokenValue> { param.Value }; 
                        continue;
                    }

                    paramsDict[param.Key].Add(param.Value);
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
                sb.Append(ValueToString(v));
            }

            var str = sb.ToString();
            _output.WriteLine($"INFO: {param.Key.Type} {param.Key.Name} values: {str}");    
        }
    }
    private string ValueToString(InformationTokenValue value)
    {
        if (value.RawValue.All(i => i == 0))
        {
            return "0";
        }

        switch (value.Type)
        {
            case ULogDataType.UInt32:
            case ULogDataType.Int32:
                return BitConverter.ToInt32(value.RawValue).ToString(CultureInfo.InvariantCulture);
            case ULogDataType.Char:
                 return CharToString(value).ToString();
            default:
                throw new ArgumentNullException("Wrong ulog value type for InformationTokenValue");
        }
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
    [InlineData(ULog.UInt32TypeName, "data", 24)]
    [InlineData(ULog.Int32TypeName, "data", 12)]
    [InlineData(ULog.CharTypeName, "data", 'd')]
    public void DeserializeToken_Success(string type, string name, ValueType value)
    {
        var readOnlySpan = SetUpTestData(type, name, value);
        var token = new ULogInformationMessageToken();
        token.Deserialize(ref readOnlySpan);
        Assert.Equal(type, ULog.GetDataTypeName(token.Key.Type, null));
        Assert.Equal(name, token.Key.Name);
        Assert.Equal(value, InformationTokenValueToValueType(token.Value));
    }

    [Theory]
    [InlineData(ULog.Int32TypeName, "%@#", 523)]
    [InlineData(ULog.CharTypeName, "`!!!`````````", 'd')]
    [InlineData(ULog.UInt32TypeName, "", 523)]
    [InlineData(ULog.Int32TypeName, null, 532)]
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
    [InlineData(ULog.CharTypeName, "data", 12f)]
    [InlineData(ULog.Int32TypeName, "data", 3535)]
    [InlineData(ULog.UInt32TypeName, "data", 3535)]
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
    [InlineData(ULog.Int32TypeName, "data", 123)]
    [InlineData(ULog.UInt32TypeName, "data", 321)]
    [InlineData(ULog.CharTypeName, "data", 'd')]
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
        var key = type + ULog.TypeAndNameSeparator + name;
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
        var key = type + ULog.TypeAndNameSeparator + name;
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

    private ValueType InformationTokenValueToValueType(InformationTokenValue value)
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