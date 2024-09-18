using System;
using Asv.IO.TaggedLoggedString;
using Xunit;

namespace Asv.IO.Test;

public class ULogTaggedLoggedStringMessageTokenTests
{
    #region Deserialize

    [Theory]
    [InlineData(0, ULogTaggedLoggedStringMessageToken.MessageTag.Unassigned, 12345678UL, "Test message")]
    [InlineData(1, ULogTaggedLoggedStringMessageToken.MessageTag.MavlinkHandler, 987654321UL, "Another test")]
    [InlineData(7, ULogTaggedLoggedStringMessageToken.MessageTag.Watchdog, 1234567890UL, "Debug info")]
    public void DeserializeToken_Success(byte logLevel, ULogTaggedLoggedStringMessageToken.MessageTag tag,
        ulong timestamp, string message)
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

    [Theory]
    [InlineData(8, ULogTaggedLoggedStringMessageToken.MessageTag.Unassigned, 12345678UL, "Test message")]
    [InlineData(0, (ULogTaggedLoggedStringMessageToken.MessageTag)10, 12345678UL, "Test message2")]
    public void DeserializeToken_InvalidData(byte logLevel, ULogTaggedLoggedStringMessageToken.MessageTag tag,
        ulong timestamp, string message)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(logLevel, tag, timestamp, message);
            var token = new ULogTaggedLoggedStringMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }

    #endregion

    #region Serialize

    [Theory]
    [InlineData(0, ULogTaggedLoggedStringMessageToken.MessageTag.Unassigned, 12345678UL, "Test message")]
    [InlineData(1, ULogTaggedLoggedStringMessageToken.MessageTag.MavlinkHandler, 987654321UL, "Another test")]
    [InlineData(7, ULogTaggedLoggedStringMessageToken.MessageTag.Watchdog, 1234567890UL, "Debug info")]
    public void SerializeToken_Success(byte logLevel, ULogTaggedLoggedStringMessageToken.MessageTag tag,
        ulong timestamp, string message)
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
    [InlineData(0, ULogTaggedLoggedStringMessageToken.MessageTag.Unassigned, 12345678UL, "Test message")]
    [InlineData(1, ULogTaggedLoggedStringMessageToken.MessageTag.MavlinkHandler, 987654321UL, "Another test")]
    [InlineData(7, ULogTaggedLoggedStringMessageToken.MessageTag.Watchdog, 1234567890UL, "Debug info")]
    public void GetByteSize_Success(byte logLevel, ULogTaggedLoggedStringMessageToken.MessageTag tag,
        ulong timestamp, string message)
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

    private static ULogTaggedLoggedStringMessageToken SetUpTestToken(byte logLevel,
        ULogTaggedLoggedStringMessageToken.MessageTag tag, ulong timestamp, string message)
    {
        return new ULogTaggedLoggedStringMessageToken
        {
            LogLevel = logLevel,
            Tag = tag,
            Timestamp = timestamp,
            Message = message
        };
    }

    private static ReadOnlySpan<byte> SetUpTestData(byte logLevel, ULogTaggedLoggedStringMessageToken.MessageTag tag,
        ulong timestamp, string message)
    {
        var token = SetUpTestToken(logLevel, tag, timestamp, message);
        var buffer = new Span<byte>(new byte[token.GetByteSize()]);
        var temp = buffer;
        token.Serialize(ref temp);
        return new ReadOnlySpan<byte>(buffer.ToArray());
    }
}