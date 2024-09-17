using System;
using System.Buffers;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test;

public class ULogSubscriptionMessageTokenTests
{
    private readonly ITestOutputHelper _output;

    public ULogSubscriptionMessageTokenTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ReadHeader()
    {
        var data = new ReadOnlySequence<byte>(TestData.ulog_log_small);
        var rdr = new SequenceReader<byte>(data);
        var reader = ULog.CreateReader();

        var result = reader.TryRead<ULogFileHeaderToken>(ref rdr, out var header);
        Assert.True(result);
        Assert.NotNull(header);
        Assert.Equal(ULogToken.FileHeader, header.Type);
        Assert.Equal(20309082U, header.Timestamp);
        Assert.Equal(1, header.Version);

        result = reader.TryRead<ULogFlagBitsMessageToken>(ref rdr, out var flag);
        Assert.True(result);
        Assert.NotNull(flag);
        Assert.Equal(ULogToken.FlagBits, flag.Type);

        while (reader.TryRead(ref rdr, out var token))
        {
            Assert.NotNull(token);
            if (token.Type == ULogToken.Subscription)
            {
                var format = token as ULogSubscriptionMessageToken;
                _output.WriteLine(
                    $"{format.Type:G}: {string.Join(",", format.Fields.MessageName)} ({string.Join(",", format.Fields.MultiId)} {string.Join(",", format.Fields.GetByteSize())})");
            }
        }
    }

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
        Assert.Equal(multiId, token.Fields.MultiId);
        Assert.Equal(messageId, token.Fields.MessageId);
        Assert.Equal(messageName, token.Fields.MessageName);
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
        var token = new ULogSubscriptionMessageToken();
        token.Fields = new SubscriptionMessageFields()
        {
            MessageId = messageId,
            MultiId = multiId,
            MessageName = messageName
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