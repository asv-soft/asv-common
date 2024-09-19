using System;
using Xunit;

namespace Asv.IO.Test;

public class ULogUnsubscriptionMessageTokenTests
{
    # region Deserialize

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(ushort.MaxValue)]
    public void Deserialize_TurnsByteArrayIntoToken_Success(ushort msgId)
    {
        // Arrange
        var readOnlySpan = SetUpTestData(msgId);
        var token = new ULogUnsubscriptionMessageToken();

        // Act
        token.Deserialize(ref readOnlySpan);

        // Assert
        Assert.Equal(msgId, token.MessageId);
    }

    # endregion

    # region Serialize

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(ushort.MaxValue)]
    public void Serialize_TurnsTokenIntoByteArray_Success(ushort msgId)
    {
        // Arrange
        var readOnlySpan = SetUpTestData(msgId);
        var token = SetUpTestToken(msgId);

        // Act
        var span = new Span<byte>(new byte[readOnlySpan.Length]);
        var temp = span;
        token.Serialize(ref temp);

        // Assert
        Assert.True(span.SequenceEqual(readOnlySpan));
    }

    # endregion

    # region GetByteSize

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(ushort.MaxValue)]
    public void GetByteSize_GetsSizeOfToken_Success(ushort msgId)
    {
        // Arrange
        var setup = SetUpTestData(msgId);
        var token = SetUpTestToken(msgId);

        // Act
        var size = token.GetByteSize();

        // Assert
        Assert.Equal(setup.Length, size);
    }

    # endregion

    #region Setup

    private ULogUnsubscriptionMessageToken SetUpTestToken(ushort msgId)
    {
        var token = new ULogUnsubscriptionMessageToken
        {
            MessageId = msgId
        };

        return token;
    }

    private ReadOnlySpan<byte> SetUpTestData(ushort msgId)
    {
        var buffer = new Span<byte>(new byte[sizeof(ushort)]);
        var temp = buffer;
        BitConverter.GetBytes(msgId).CopyTo(temp);

        var byteArray = buffer.ToArray();
        var readOnlySpan = new ReadOnlySpan<byte>(byteArray);

        return readOnlySpan;
    }

    #endregion
}