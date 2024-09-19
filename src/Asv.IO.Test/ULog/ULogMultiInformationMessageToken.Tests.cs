using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test;

public class ULogMultiInformationMessageToken
{
    private readonly ITestOutputHelper _output;

    public ULogMultiInformationMessageToken(ITestOutputHelper output)
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
        Assert.Equal(ULogToken.FileHeader,header.TokenType);
        Assert.Equal(20309082U, header.Timestamp);
        Assert.Equal(1,header.Version);
        result = reader.TryRead<ULogFlagBitsMessageToken>(ref rdr, out var flag);
        Assert.True(result);
        Assert.NotNull(flag);  
        Assert.Equal(ULogToken.FlagBits,flag.TokenType);

        var paramsDict = new Dictionary<string, IList<(ULogType,byte[])>>();
        while (reader.TryRead(ref rdr, out var token))
        {
            if (token is IO.ULogMultiInformationMessageToken param)
            {
                if (!paramsDict.ContainsKey(param.Key.Name))
                {
                    paramsDict[param.Key.Name] = new List<(ULogType,byte[])> { new (param.Key.Type.BaseType,param.Value) }; 
                    continue;
                }

                paramsDict[param.Key.Name].Add(new (param.Key.Type.BaseType,param.Value));
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
    [InlineData(0, ULogTypeDefinition.UInt32TypeName, "data", 24U)]
    [InlineData(1, ULogTypeDefinition.Int32TypeName, "data", 12)]
    [InlineData(1, ULogTypeDefinition.CharTypeName, "data", 'd')]
    public void Multi_DeserializeToken_Success(byte isContinued, string type, string name, ValueType value)
    {
        var readOnlySpan = SetUpTestData(isContinued, type, name, value);
        var token = new IO.ULogMultiInformationMessageToken();
        token.Deserialize(ref readOnlySpan);
        Assert.Equal(type, token.Key.Type.TypeName);
        Assert.Equal(name, token.Key.Name);
        Assert.Equal(value, ULog.GetSimpleValue(token.Key.Type.BaseType, token.Value));
    }

    [Theory]
    [InlineData(0, ULogTypeDefinition.Int32TypeName, "%@#", 523)]
    [InlineData(1, ULogTypeDefinition.CharTypeName, "`!!!`````````", 'd')]
    [InlineData(1, ULogTypeDefinition.UInt32TypeName, "", 523)]
    [InlineData(1, ULogTypeDefinition.Int32TypeName, null, 532)]
    public void Multi_DeserializeToken_WrongKeyName(byte isContinued, string type, string name, ValueType value)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(isContinued, type, name, value);
            var token = new IO.ULogMultiInformationMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }

    [Theory]
    [InlineData(0, ULogTypeDefinition.CharTypeName, "data", 12f)]
    [InlineData(1, ULogTypeDefinition.Int32TypeName, "data", 3535)]
    [InlineData(0, ULogTypeDefinition.UInt32TypeName, "data", 3535)]
    public void Multi_DeserializeToken_NoKeyBytes(byte isContinued, string type, string name, ValueType value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var readOnlySpan = SetUpTestDataWithoutKeyLength(isContinued, type, name, value);
            var token = new IO.ULogMultiInformationMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }

    [Theory]
    [InlineData(0, ULogTypeDefinition.Int32TypeName, "data", 123)]
    [InlineData(1, ULogTypeDefinition.UInt32TypeName, "data", 321)]
    [InlineData(0, ULogTypeDefinition.CharTypeName, "data", 'd')]
    public void Multi_DeserializeToken_WrongKeyBytes(byte isContinued, string type, string name, ValueType value)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(isContinued, type, name, value, 0);
            var token = new IO.ULogMultiInformationMessageToken();
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
            var token = new IO.ULogMultiInformationMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }

    private ReadOnlySpan<byte> SetUpTestDataWithoutKeyLength(byte isContinued, string type, string name, ValueType value)
    {
        var key = type + ULogTypeAndNameDefinition.TypeAndNameSeparator + name;
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
        var key = type + ULogTypeAndNameDefinition.TypeAndNameSeparator + name;
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

    
}