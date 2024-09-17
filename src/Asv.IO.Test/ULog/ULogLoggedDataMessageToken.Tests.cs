using System;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test;

public class ULogLoggedDataMessageTokenTests
{
    #region Deserialize
    
    [Theory]
    [InlineData(0, ULogTypeDefinition.FloatTypeName, "data")]
    [InlineData(1, ULogTypeDefinition.Int32TypeName, "value")]
    [InlineData(42, ULogTypeDefinition.FloatTypeName, "speed")]
    public void DeserializeToken_Success(ushort messageId, string type, string name)
    {
        // Arrange
        var readOnlySpan = SetUpTestData(messageId, type, name);
        var token = new ULogLoggedDataMessageToken();
        
        // Act
        token.Deserialize(ref readOnlySpan);
        
        // Assert
        Assert.Equal(messageId, token.MessageId);
        Assert.Equal(type, token.Data.Type.TypeName);
        Assert.Equal(name, token.Data.Name);
    }
    
    [Theory]
    [InlineData(1, ULogTypeDefinition.FloatTypeName, null)]
    public void DeserializeToken_WrongData(ushort messageId, string type, string name)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(messageId, type, name);
            var token = new ULogLoggedDataMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }
    
    #endregion
    
    #region Serialize
    
    [Theory]
    [InlineData(0, ULogTypeDefinition.FloatTypeName, "data")]
    [InlineData(1, ULogTypeDefinition.Int32TypeName, "value")]
    public void SerializeToken_Success(ushort messageId, string type, string name)
    {
        // Arrange
        var readOnlySpan = SetUpTestData(messageId, type, name);
        var token = SetUpTestToken(messageId, type, name);
        
        // Act
        var span = new Span<byte>(new byte[readOnlySpan.Length]);
        var temp = span;
        token.Serialize(ref temp);
        
        // Assert
        Assert.True(span.SequenceEqual(readOnlySpan));
    }
    
    [Theory]
    [InlineData(0, ULogTypeDefinition.FloatTypeName, null)]
    public void SerializeToken_WrongData(ushort messageId, string type, string name)
    {
        Assert.Throws<ULogException>(() =>
        {
            var token = SetUpTestToken(messageId, type, name);
            var span = new Span<byte>(new byte[1024]);
            var temp = span;
            token.Serialize(ref temp);
        });
    }
    
    #endregion

    #region GetByteSize
    
    [Theory]
    [InlineData(0, ULogTypeDefinition.FloatTypeName, "data")]
    [InlineData(1, ULogTypeDefinition.Int32TypeName, "value")]
    public void GetByteSize_Success(ushort messageId, string type, string name)
    {
        // Arrange
        var setup = SetUpTestData(messageId, type, name);
        var token = SetUpTestToken(messageId, type, name);
        
        // Act
        var size = token.GetByteSize();
        
        // Assert
        Assert.Equal(setup.Length, size);
    }
    
    #endregion
    private static ULogLoggedDataMessageToken SetUpTestToken(ushort messageId, string type, string name)
    {
        var token = new ULogLoggedDataMessageToken
        {
            MessageId = messageId,
            Data = new ULogTypeAndNameDefinition
            {
                Type = new ULogTypeDefinition
                {
                    TypeName = type,
                    BaseType = type == ULogTypeDefinition.FloatTypeName ? ULogType.Float : ULogType.Int32
                },
                Name = name
            }
        };
        return token;
    }

    private ReadOnlySpan<byte> SetUpTestData(ushort messageId, string type, string name)
    {
        var token = SetUpTestToken(messageId, type, name);
        var buffer = new Span<byte>(new byte[token.GetByteSize()]);
        var temp = buffer;
        token.Serialize(ref temp);
        return new ReadOnlySpan<byte>(buffer.ToArray());
    }

}