using System;
using System.Buffers;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test;

public class ULogLoggedStringMessageTokenTests
{
    #region Deserialize

    [Theory]
    [InlineData(ULogLoggedStringMessageToken.ULogLevel.Emerg, 12345678UL, "Test message")]
    [InlineData(ULogLoggedStringMessageToken.ULogLevel.Debug, 987654321UL, "Another test")]
    [InlineData(ULogLoggedStringMessageToken.ULogLevel.Info, 1234567890UL, "Debug info")]
    public void DeserializeToken_Success(ULogLoggedStringMessageToken.ULogLevel logLevel, ulong timestamp, string message)
    {
        // Arrange
        var readOnlySpan = SetUpTestData(logLevel, timestamp, message);
        var token = new ULogLoggedStringMessageToken();

        // Act
        token.Deserialize(ref readOnlySpan);

        // Assert
        Assert.Equal(logLevel, token.LogLevel);
        Assert.Equal(timestamp, token.TimeStamp);
        Assert.Equal(message, token.Message);
    }

    #endregion

    #region Serialize

    [Theory]
    [InlineData(ULogLoggedStringMessageToken.ULogLevel.Emerg, 12345678UL, "Test message")]
    [InlineData(ULogLoggedStringMessageToken.ULogLevel.Debug, 987654321UL, "Another test")]
    [InlineData(ULogLoggedStringMessageToken.ULogLevel.Info, 1234567890UL, "Debug info")]
    public void SerializeToken_Success(ULogLoggedStringMessageToken.ULogLevel logLevel, ulong timestamp, string message)
    {
        // Arrange
        var readOnlySpan = SetUpTestData(logLevel, timestamp, message);
        var token = SetUpTestToken(logLevel, timestamp, message);

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
    [InlineData(ULogLoggedStringMessageToken.ULogLevel.Emerg, 12345678UL, "Test message")]
    [InlineData(ULogLoggedStringMessageToken.ULogLevel.Debug, 987654321UL, "Another test")]
    [InlineData(ULogLoggedStringMessageToken.ULogLevel.Info, 1234567890UL, "Debug info")]
    public void GetByteSize_Success(ULogLoggedStringMessageToken.ULogLevel logLevel, ulong timestamp, string message)
    {
        // Arrange
        var setup = SetUpTestData(logLevel, timestamp, message);
        var token = SetUpTestToken(logLevel, timestamp, message);

        // Act
        var size = token.GetByteSize();

        // Assert
        Assert.Equal(setup.Length, size);
    }

    #endregion

    private static ULogLoggedStringMessageToken SetUpTestToken(ULogLoggedStringMessageToken.ULogLevel logLevel, ulong timestamp, string message)
    {
        return new ULogLoggedStringMessageToken
        {
            LogLevel = logLevel,
            TimeStamp = timestamp,
            Message = message
        };
    }

    private static ReadOnlySpan<byte> SetUpTestData(ULogLoggedStringMessageToken.ULogLevel logLevel, ulong timestamp, string message)
    {
        var token = new ULogLoggedStringMessageToken
        {
            LogLevel = logLevel,
            TimeStamp = timestamp,
            Message = message
        };
        var buffer = new Span<byte>(new byte[token.GetByteSize()]);
        var temp = buffer;
        token.Serialize(ref temp);
        return new ReadOnlySpan<byte>(buffer.ToArray());
    }
}