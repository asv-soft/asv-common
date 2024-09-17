using System;
using Xunit;

namespace Asv.IO.Test;

public class ULogParameterMessageTokenTests
{
    # region Deserialize
    [Theory]
    [InlineData(ULogTypeDefinition.Int32TypeName, "data", 24)]
    [InlineData(ULogTypeDefinition.Int32TypeName, "fdata1234", 12)]
    [InlineData(ULogTypeDefinition.FloatTypeName, "data1", 24.21f)]
    [InlineData(ULogTypeDefinition.FloatTypeName, "data1", 12.01f)]
    public void DeserializeToken_Success(string type, string name, ValueType value)
    {
        // Arrange
        var readOnlySpan = SetUpTestData(type, name, value);
        var token = new ULogParameterMessageToken();
        
        // Act
        token.Deserialize(ref readOnlySpan);
        
        // Assert
        Assert.Equal(type, token.Key.Type.TypeName);
        Assert.Equal(name, token.Key.Name);
        
        if (value is float expected && ParameterTokenValueToValueType(token.Key.Type.BaseType, token.Value) is float actual)
        {
            var tolerance = 1e-9 * Math.Max(actual, expected);
            Assert.InRange(actual - expected, -tolerance, tolerance);
        }
        Assert.Equal(value, ParameterTokenValueToValueType(token.Key.Type.BaseType,token.Value));
    }
    
    [Theory]
    [InlineData(ULogTypeDefinition.UInt8TypeName, "data", 24)]
    [InlineData(ULogTypeDefinition.Int16TypeName, "data", 24)]
    [InlineData(ULogTypeDefinition.UInt16TypeName, "data", 24)]
    [InlineData(ULogTypeDefinition.UInt32TypeName, "data", 24)]
    [InlineData(ULogTypeDefinition.Int64TypeName, "data", 24)]
    [InlineData(ULogTypeDefinition.UInt64TypeName, "data", 24)]
    [InlineData(ULogTypeDefinition.DoubleTypeName, "data", 24)]
    [InlineData(ULogTypeDefinition.BoolTypeName, "data", 24)]
    [InlineData(ULogTypeDefinition.CharTypeName, "data", 24)]
    public void DeserializeToken_WrongULogType(string type, string name, ValueType value)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(type, name, value);
            var token = new ULogParameterMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }
    
    [Theory]
    [InlineData(ULogTypeDefinition.FloatTypeName, "%@#", 12.01f)]
    [InlineData(ULogTypeDefinition.FloatTypeName, "`!!!`````````", 12.01f)]
    [InlineData(ULogTypeDefinition.FloatTypeName, "", 12.01f)]
    [InlineData(ULogTypeDefinition.FloatTypeName, null, 12.01f)]
    public void DeserializeToken_WrongKeyName(string type, string name, ValueType value)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(type, name, value);
            var token = new ULogParameterMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }
    
    [Theory]
    [InlineData(ULogTypeDefinition.FloatTypeName, "data", 12f)]
    [InlineData(ULogTypeDefinition.FloatTypeName, "data", 3535.455f)]
    public void DeserializeToken_NoKeyBytes(string type, string name, ValueType value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var readOnlySpan = SetUpTestDataWithoutKeyLength(type, name, value);
            var token = new ULogParameterMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }
    
    [Theory]
    [InlineData(ULogTypeDefinition.FloatTypeName, "data", 12f)]
    [InlineData(ULogTypeDefinition.FloatTypeName, "data", 3535.455f)]
    public void DeserializeToken_WrongKeyBytes(string type, string name, ValueType value)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(type, name, value, 0);
            var token = new ULogParameterMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }
    
    [Theory]
    [InlineData(null, "data", 12f)]
    [InlineData(null, "data", 3535.455f)]
    public void DeserializeToken_NoType(string type, string name, ValueType value)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(type, name, value, 0);
            var token = new ULogParameterMessageToken();
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
            float floatValue => BitConverter.GetBytes(floatValue),
            Int32 int32Value => BitConverter.GetBytes(int32Value),
            double doubleValue => BitConverter.GetBytes(doubleValue),
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
    
    private ReadOnlySpan<byte> SetUpTestData(string type, string name, ValueType value, byte? kLength = null)
    {
        var key = type + ULogTypeAndNameDefinition.TypeAndNameSeparator + name;
        var keyLength = kLength ?? (byte)key.Length;
        
        var keyBytes = ULog.Encoding.GetBytes(key);

        byte[] valueBytes = value switch
        {
            float floatValue => BitConverter.GetBytes(floatValue),
            Int32 int32Value => BitConverter.GetBytes(int32Value),
            double doubleValue => BitConverter.GetBytes(doubleValue),
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
    
    private ValueType ParameterTokenValueToValueType(ULogType typeBaseType, byte[] value)
    {
        switch (typeBaseType)
        {
            case ULogType.Float:
                var single = BitConverter.ToSingle(value);
                return single;
            case ULogType.Int32:
                var int32 = BitConverter.ToInt32(value);
                return int32;
            default:
                throw new ArgumentException("Wrong ulog value type for ParameterTokenValue");
        }
    }
}