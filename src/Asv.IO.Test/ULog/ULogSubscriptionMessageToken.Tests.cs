using System;
using System.Buffers;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test;

public class ULogSubscriptionMessageTokenTests
{
    # region Deserialize

    [Theory]
    [InlineData(1, 14, "data")]
    [InlineData(byte.MaxValue, ushort.MaxValue, "data")]
    [InlineData(byte.MinValue, ushort.MinValue, "data")]
    [InlineData(byte.MaxValue, ushort.MinValue, "data")]
    [InlineData(byte.MinValue, ushort.MaxValue, "data")]
    public void DeserializeToken_Success(byte multiId, ushort messageId, string messageName)
    {
        // Arrange
        var readOnlySpan = SetUpTestData(multiId, messageId, messageName);
        var token = new ULogSubscriptionMessageToken();

        // Act
        token.Deserialize(ref readOnlySpan);

        // Assert
        Assert.Equal(multiId, token.MultiId);
        Assert.Equal(messageId, token.MessageId);
        Assert.Equal(messageName, token.MessageName);
    }

    [Theory]
    [InlineData(1, 14, null)]
    [InlineData(byte.MaxValue, ushort.MaxValue, null)]
    [InlineData(byte.MinValue, ushort.MinValue, null)]
    public void DeserializeToken_NoMessage(byte multiId, ushort messageId, string messageName)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(multiId, messageId, messageName);
            var token = new ULogSubscriptionMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }

    [Theory]
    [InlineData(1, 14, "%@#")]
    [InlineData(byte.MaxValue, ushort.MaxValue, "`!!!`````````")]
    [InlineData(byte.MinValue, ushort.MinValue, "")]
    public void DeserializeToken_WrongMessageName(byte multiId, ushort messageId, string messageName)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(multiId, messageId, messageName);
            var token = new ULogSubscriptionMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }

    # endregion

    # region Serialize

    [Theory]
    [InlineData(1, 14, "data")]
    [InlineData(byte.MaxValue, ushort.MaxValue, "data")]
    [InlineData(byte.MinValue, ushort.MinValue, "data")]
    [InlineData(byte.MaxValue, ushort.MinValue, "data")]
    [InlineData(byte.MinValue, ushort.MaxValue, "data")]
    public void SerializeToken_Success(byte multiId, ushort messageId, string messageName)
    {
        // Arrange
        var readOnlySpan = SetUpTestData(multiId, messageId, messageName);
        var token = SetUpTestToken(multiId, messageId, messageName);

        // Act
        var span = new Span<byte>(new byte[readOnlySpan.Length]);
        var temp = span;
        token.Serialize(ref temp);

        // Assert
        Assert.True(span.SequenceEqual(readOnlySpan));
    }

    [Theory]
    [InlineData(1, 14, "%@#")]
    [InlineData(byte.MaxValue, ushort.MaxValue, "`!!!`````````")]
    [InlineData(byte.MinValue, ushort.MinValue, "")]
    public void SerializeToken_WrongMessageName(byte multiId, ushort messageId, string messageName)
    {
        Assert.Throws<ULogException>(() =>
        {
            var readOnlySpan = SetUpTestData(multiId, messageId, messageName);
            var token = SetUpTestToken(multiId, messageId, messageName);
            var span = new Span<byte>(new byte[readOnlySpan.Length]);
            var temp = span;
            token.Serialize(ref temp);
        });
    }

    # endregion

    # region GetByteSize

    [Theory]
    [InlineData(1, 14, "data")]
    [InlineData(byte.MaxValue, ushort.MaxValue, "data")]
    [InlineData(byte.MinValue, ushort.MinValue, "data")]
    [InlineData(byte.MaxValue, ushort.MinValue, "data")]
    [InlineData(byte.MinValue, ushort.MaxValue, "data")]
    public void GetByteSize_Success(byte multiId, ushort messageId, string messageName)
    {
        // Arrange
        var setup = SetUpTestData(multiId, messageId, messageName);
        var token = SetUpTestToken(multiId, messageId, messageName);

        // Act
        var size = token.GetByteSize();

        // Assert
        Assert.Equal(setup.Length, size);
    }

    # endregion

    #region Setup

    private ULogSubscriptionMessageToken SetUpTestToken(byte multiId, ushort messageId, string messageName)
    {
        var token = new ULogSubscriptionMessageToken
        {
            MessageName = messageName,
            MessageId = messageId,
            MultiId = multiId
        };
        return token;
    }

    private ReadOnlySpan<byte> SetUpTestData(byte multiId, ushort messageId, string messageName)
    {
        if (messageName is null)
        {
            throw new ULogException("Value cannot be null");
        }

        var messageIdBytes = BitConverter.GetBytes(messageId);
        var messageNameBytes = ULog.Encoding.GetBytes(messageName);
        var buffer = new Span<byte>(new byte[1 + 2 + messageNameBytes.Length]);

        buffer[0] = multiId;
        messageIdBytes.CopyTo(buffer.Slice(1, 2));

        for (var i = 0; i < messageNameBytes.Length; i++)
        {
            buffer[i + 3] = messageNameBytes[i];
        }

        var byteArray = buffer.ToArray();
        var readOnlySpan = new ReadOnlySpan<byte>(byteArray);

        return readOnlySpan;
    }

    #endregion
}