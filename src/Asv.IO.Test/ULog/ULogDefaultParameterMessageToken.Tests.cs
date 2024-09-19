using System;
using Xunit;

namespace Asv.IO.Test;

public class ULogDefaultParameterMessageTokenTests
{
    # region Deserialize
    
    [Theory]
    [InlineData(1, ULogTypeDefinition.Int32TypeName, "data", 24)]
    [InlineData(2, ULogTypeDefinition.Int32TypeName, "fdata1234", 12)]
    [InlineData(1, ULogTypeDefinition.FloatTypeName, "data1", float.MaxValue)]
    [InlineData(2, ULogTypeDefinition.FloatTypeName, "data1", float.MinValue)]
    [InlineData(3, ULogTypeDefinition.FloatTypeName, "serdata11", 0f)]
    public void DeserializeToken_Success(byte defaultType, string type, string name, ValueType value)
    {
        // Arrange
        var readOnlySpan = SetUpTestData(defaultType, type, name, value);
        var token = new ULogDefaultParameterMessageToken();
        
        // Act
        token.Deserialize(ref readOnlySpan);
        
        // Assert
        Assert.Equal(type, token.Key.Type.TypeName);
        Assert.Equal(name, token.Key.Name);
        Assert.Equal((ULogParameterDefaultTypes) defaultType, token.DefaultType);
        
        if (value is float expected && ParameterTokenValueToValueType(token.Key.Type.BaseType, token.Value) is float actual)
        {
            var tolerance = 0.00000001f;
            Assert.InRange(Math.Abs(actual - expected), -tolerance, tolerance);
        }
        Assert.Equal(value, ParameterTokenValueToValueType(token.Key.Type.BaseType, token.Value));
    }
    
    [Theory]
    [InlineData(0, ULogTypeDefinition.Int32TypeName, "data", 24)]
    public void DeserializeToken_WrongDefaultType(byte defaultType, string type, string name, ValueType value)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(defaultType, type, name, value);
            var token = new ULogDefaultParameterMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }
    
    [Theory]
    [InlineData(1, ULogTypeDefinition.UInt8TypeName, "data", 24)]
    [InlineData(1, ULogTypeDefinition.Int16TypeName, "data", 24)]
    [InlineData(1, ULogTypeDefinition.UInt16TypeName, "data", 24)]
    [InlineData(1, ULogTypeDefinition.UInt32TypeName, "data", 24)]
    [InlineData(1, ULogTypeDefinition.Int64TypeName, "data", 24)]
    [InlineData(1, ULogTypeDefinition.UInt64TypeName, "data", 24)]
    [InlineData(1, ULogTypeDefinition.DoubleTypeName, "data", 24)]
    [InlineData(1, ULogTypeDefinition.BoolTypeName, "data", 24)]
    [InlineData(1, ULogTypeDefinition.CharTypeName, "data", 24)]
    public void DeserializeToken_WrongULogType(byte defaultType, string type, string name, ValueType value)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(defaultType, type, name, value);
            var token = new ULogDefaultParameterMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }
    
    [Theory]
    [InlineData(1, ULogTypeDefinition.FloatTypeName, "%@#", 12.01f)]
    [InlineData(1, ULogTypeDefinition.FloatTypeName, "`!!!`````````", 12.01f)]
    [InlineData(1, ULogTypeDefinition.FloatTypeName, "", 12.01f)]
    [InlineData(1, ULogTypeDefinition.FloatTypeName, null, 12.01f)]
    public void DeserializeToken_WrongKeyName(byte defaultType, string type, string name, ValueType value)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(defaultType, type, name, value);
            var token = new ULogDefaultParameterMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }
    
    [Theory]
    [InlineData(1, ULogTypeDefinition.FloatTypeName, "data", 12f)]
    [InlineData(1, ULogTypeDefinition.FloatTypeName, "data", 3535.455f)]
    public void DeserializeToken_WrongKeyBytes(byte defaultType, string type, string name, ValueType value)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(defaultType, type, name, value, 0);
            var token = new ULogDefaultParameterMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }
    
    [Theory]
    [InlineData(1, null, "data", 12f)]
    [InlineData(1, null, "data", 3535.455f)]
    public void DeserializeToken_NoType(byte defaultType, string type, string name, ValueType value)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(defaultType, type, name, value, 0);
            var token = new ULogDefaultParameterMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }
    
    # endregion
    
    # region Serialize
    
    [Theory]
    [InlineData(ULogParameterDefaultTypes.SystemWide, ULogTypeDefinition.FloatTypeName, "data", float.MaxValue)]
    [InlineData(ULogParameterDefaultTypes.SystemWide, ULogTypeDefinition.FloatTypeName, "data", float.MinValue)]
    [InlineData(ULogParameterDefaultTypes.SystemWide, ULogTypeDefinition.Int32TypeName, "data", Int32.MaxValue)]
    [InlineData(ULogParameterDefaultTypes.SystemWide, ULogTypeDefinition.Int32TypeName, "data", Int32.MinValue)]
    [InlineData(ULogParameterDefaultTypes.SystemWide, ULogTypeDefinition.FloatTypeName, "data", 0f)]
    [InlineData(ULogParameterDefaultTypes.SystemWide, ULogTypeDefinition.Int32TypeName, "data", 0)]
    [InlineData(ULogParameterDefaultTypes.SystemWide | ULogParameterDefaultTypes.ForCurrentConfiguration, ULogTypeDefinition.Int32TypeName, "data", 0)]
    [InlineData(ULogParameterDefaultTypes.ForCurrentConfiguration, ULogTypeDefinition.Int32TypeName, "data", 0)]
    public void SerializeToken_Success(ULogParameterDefaultTypes defaultType, string type, string name, ValueType value)
    {
        // Arrange
        var readOnlySpan = SetUpTestData((byte) defaultType, type, name, value);
        var token = SetUpTestToken(defaultType, type, name, value);
        
        // Act
        var span = new Span<byte>(new byte[readOnlySpan.Length]);
        var temp = span;
        token.Serialize(ref temp);
        
        // Assert
        Assert.True(span.SequenceEqual(readOnlySpan));
    }
    
    [Theory]
    [InlineData(ULogParameterDefaultTypes.SystemWide, ULogTypeDefinition.FloatTypeName, "data", 0d)]
    [InlineData(ULogParameterDefaultTypes.SystemWide, ULogTypeDefinition.Int32TypeName, "data", 0.1)]
    public void SerializeToken_WrongValue(ULogParameterDefaultTypes defaultType, string type, string name, ValueType value)
    {
        Assert.Throws<InvalidCastException>(() =>
        {
            var readOnlySpan = SetUpTestData((byte)defaultType, type, name, value);
            var token = SetUpTestToken(defaultType, type, name, value);
            var span = new Span<byte>(new byte[readOnlySpan.Length]);
            var temp = span;
            token.Serialize(ref temp);
        });
    }
    
    [Theory]
    [InlineData(ULogParameterDefaultTypes.None, ULogTypeDefinition.FloatTypeName, "data", 0f)]
    public void SerializeToken_WrongDefaultType(ULogParameterDefaultTypes defaultType, string type, string name, ValueType value)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData((byte)defaultType, type, name, value);
            var token = SetUpTestToken(defaultType, type, name, value);
            var span = new Span<byte>(new byte[readOnlySpan.Length]);
            var temp = span;
            token.Serialize(ref temp);
        });
    }
    
    # endregion
    
    # region GetByteSize
    
    [Theory]
    [InlineData(ULogParameterDefaultTypes.SystemWide, ULogTypeDefinition.FloatTypeName, "data", float.MaxValue)]
    [InlineData(ULogParameterDefaultTypes.SystemWide, ULogTypeDefinition.FloatTypeName, "data", float.MinValue)]
    [InlineData(ULogParameterDefaultTypes.SystemWide, ULogTypeDefinition.Int32TypeName, "data", Int32.MaxValue)]
    [InlineData(ULogParameterDefaultTypes.SystemWide, ULogTypeDefinition.Int32TypeName, "data", Int32.MinValue)]
    [InlineData(ULogParameterDefaultTypes.SystemWide, ULogTypeDefinition.FloatTypeName, "data", 0f)]
    [InlineData(ULogParameterDefaultTypes.SystemWide, ULogTypeDefinition.Int32TypeName, "data", 0)]
    public void GetByteSize_Success(ULogParameterDefaultTypes defaultType, string type, string name, ValueType value)
    {
        // Arrange
        var setup = SetUpTestData((byte) defaultType, type, name, value);
        var token = SetUpTestToken(defaultType, type, name, value);
        
        // Act
        var size = token.GetByteSize();
        
        // Assert
        Assert.Equal(setup.Length, size);
    }
    
    # endregion

    #region Setup
    
    private ULogDefaultParameterMessageToken SetUpTestToken(ULogParameterDefaultTypes defaultType, string type, string name, ValueType value)
    {
        var token = new ULogDefaultParameterMessageToken();
        token.DefaultType = defaultType;
        token.Key = new ();
        
        switch (type)
        {
            case ULogTypeDefinition.Int32TypeName:
                token.Key.Type = new ULogTypeDefinition
                {
                    BaseType = ULogType.Int32,
                    TypeName = type
                };
                break;
            case ULogTypeDefinition.FloatTypeName:
                token.Key.Type = new ULogTypeDefinition
                {
                    BaseType = ULogType.Float,
                    TypeName = type
                };
                break;
        }
        
        token.Key.Name = name;

        token.Value = ValueTypeToByteArray(value, token.Key.Type.BaseType);

        return token;
    }
    
    private ReadOnlySpan<byte> SetUpTestData(byte defaultType, string type, string name, ValueType value, byte? kLength = null)
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

        var buffer = new Span<byte>(new byte[2 + ULog.Encoding.GetByteCount(key) + valueBytes.Length]);
        buffer[0] = defaultType;
        buffer[1] = keyLength;
        
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
    
    private byte[] ValueTypeToByteArray(ValueType value, ULogType dataType)
    {
        switch (dataType)
        {
            case ULogType.Float:
                var floatBytes = BitConverter.GetBytes((float) value);
                return floatBytes;
            case ULogType.Int32:
                var intBytes = BitConverter.GetBytes((Int32) value);
                return intBytes;
            default:
                throw new ArgumentException("Wrong ulog value type for ParameterTokenValue");
        }
    }
    
    #endregion
}