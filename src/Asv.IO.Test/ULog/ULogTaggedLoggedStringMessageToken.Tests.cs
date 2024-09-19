using System;
using Xunit;

namespace Asv.IO.Test;

public class ULogTaggedLoggedStringMessageTokenTests
{
    #region Deserialize
    
    [Theory]
    [InlineData(ULogTaggedLoggedStringMessageToken.ULogLevel.Emerg, 1, 12345678UL, "Test message")]
    [InlineData(ULogTaggedLoggedStringMessageToken.ULogLevel.Debug, 42, 987654321UL, "Another test")]
    [InlineData(ULogTaggedLoggedStringMessageToken.ULogLevel.Info, 65535, 1234567890UL, "Debug info")]
    public void DeserializeToken_Success(ULogTaggedLoggedStringMessageToken.ULogLevel logLevel, ushort tag, ulong timestamp, string message)
    {
        // Arrange
        var readOnlySpan = SetUpTestData(logLevel, tag, timestamp, message);
        var token = new ULogTaggedLoggedStringMessageToken();
        
        // Act
        token.Deserialize(ref readOnlySpan);
        
        // Assert
        Assert.Equal(logLevel, token.LogLevel);
        Assert.Equal(tag, token.Tag);
        Assert.Equal(timestamp, token.Timestamp);
        Assert.Equal(message, token.Message);
    }

    #endregion

    #region Serialize
    
    [Theory]
    [InlineData(ULogTaggedLoggedStringMessageToken.ULogLevel.Emerg, 1, 12345678UL, "Test message")]
    [InlineData(ULogTaggedLoggedStringMessageToken.ULogLevel.Debug, 42, 987654321UL, "Another test")]
    [InlineData(ULogTaggedLoggedStringMessageToken.ULogLevel.Info, 65535, 1234567890UL, "Debug info")]
    public void SerializeToken_Success(ULogTaggedLoggedStringMessageToken.ULogLevel logLevel, ushort tag, ulong timestamp, string message)
    {
        // Arrange
        var readOnlySpan = SetUpTestData(logLevel, tag, timestamp, message);
        var token = SetUpTestToken(logLevel, tag, timestamp, message);
        
        // Act
        var span = new Span<byte>(new byte[readOnlySpan.Length]);
        var temp = span;
        token.Serialize(ref temp);
        
        // Assert
        Assert.True(span.SequenceEqual(readOnlySpan));
    }

    #endregion

    #region GetByteSize
    
    [Theory]
    [InlineData(ULogTaggedLoggedStringMessageToken.ULogLevel.Emerg, 1, 12345678UL, "Test message")]
    [InlineData(ULogTaggedLoggedStringMessageToken.ULogLevel.Debug, 42, 987654321UL, "Another test")]
    [InlineData(ULogTaggedLoggedStringMessageToken.ULogLevel.Info, 65535, 1234567890UL, "Debug info")]
    public void GetByteSize_Success(ULogTaggedLoggedStringMessageToken.ULogLevel logLevel, ushort tag, ulong timestamp, string message)
    {
        // Arrange
        var setup = SetUpTestData(logLevel, tag, timestamp, message);
        var token = SetUpTestToken(logLevel, tag, timestamp, message);
        
        // Act
        var size = token.GetByteSize();
        
        // Assert
        Assert.Equal(setup.Length, size);
    }

    #endregion

    private static ULogTaggedLoggedStringMessageToken SetUpTestToken(ULogTaggedLoggedStringMessageToken.ULogLevel logLevel, 
        ushort tag, ulong timestamp, string message)
    {
        return new ULogTaggedLoggedStringMessageToken
        {
            LogLevel = logLevel,
            Tag = tag,
            Timestamp = timestamp,
            Message = message
        };
    }

    private static ReadOnlySpan<byte> SetUpTestData(ULogTaggedLoggedStringMessageToken.ULogLevel logLevel, 
        ushort tag, ulong timestamp, string message)
    {
        var token = new ULogTaggedLoggedStringMessageToken
        {
            LogLevel = logLevel,
            Tag = tag,
            Timestamp = timestamp,
            Message = message
        };
        var buffer = new Span<byte>(new byte[token.GetByteSize()]);
        var temp = buffer;
        token.Serialize(ref temp);
        return new ReadOnlySpan<byte>(buffer.ToArray());
    }
}