using System;
using System.Buffers;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test;

public class ULogLoggedDataMessageTokenTests
{
    private readonly ITestOutputHelper _output;

    public ULogLoggedDataMessageTokenTests(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Fact]
    public void ReadLoggedDataFromFile()
    {
        var data = new ReadOnlySequence<byte>(TestData.ulog_log_small);
        var rdr = new SequenceReader<byte>(data); 
        var reader = ULog.CreateReader();

        var counter = 0;
        while (reader.TryRead(ref rdr, out var token))
        {
            if (token?.TokenType != ULogToken.LoggedData) continue;
            Assert.Equal(ULogToken.LoggedData,token.TokenType);
            counter++;
        }
        _output.WriteLine($"Amount of LoggedData: {counter}");
    }
    
    #region Deserialize
    
    [Theory]
    [InlineData(1, new byte[] { 0x01, 0x02, 0x03, 0x04 })]
    [InlineData(42, new byte[] { 0xFF, 0xAA, 0xBB, 0xCC })]
    [InlineData(65535, new byte[] { 0x10, 0x20, 0x30 })]
    public void DeserializeToken_Success(ushort messageId, byte[] data)
    {
        // Arrange
        var readOnlySpan = SetUpTestData(messageId, data);
        var token = new ULogLoggedDataMessageToken();
        
        // Act
        token.Deserialize(ref readOnlySpan);
        
        // Assert
        Assert.Equal(messageId, token.MessageId);
        Assert.Equal(data, token.Data);
    }

    #endregion
    
    #region Serialize
    
    [Theory]
    [InlineData(1, new byte[] { 0x01, 0x02, 0x03, 0x04 })]
    [InlineData(42, new byte[] { 0xFF, 0xAA, 0xBB, 0xCC })]
    [InlineData(65535, new byte[] { 0x10, 0x20, 0x30 })]
    public void SerializeToken_Success(ushort messageId, byte[] data)
    {
        // Arrange
        var readOnlySpan = SetUpTestData(messageId, data);
        var token = SetUpTestToken(messageId, data);
        
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
    [InlineData(1, new byte[] { 0x01, 0x02, 0x03, 0x04 })]
    [InlineData(42, new byte[] { 0xFF, 0xAA, 0xBB, 0xCC })]
    [InlineData(65535, new byte[] { 0x10, 0x20, 0x30 })]
    public void GetByteSize_Success(ushort messageId, byte[] data)
    {
        // Arrange
        var setup = SetUpTestData(messageId, data);
        var token = SetUpTestToken(messageId, data);
        
        // Act
        var size = token.GetByteSize();
        
        // Assert
        Assert.Equal(setup.Length, size);
    }

    #endregion
    
    private static ULogLoggedDataMessageToken SetUpTestToken(ushort messageId, byte[] data)
    {
        return new ULogLoggedDataMessageToken
        {
            MessageId = messageId,
            Data = data
        };
    }

    private static ReadOnlySpan<byte> SetUpTestData(ushort messageId, byte[] data)
    {
        var buffer = new Span<byte>(new byte[sizeof(ushort) + data.Length]);
        var temp = buffer;
        var token = new ULogLoggedDataMessageToken
        {
            MessageId = messageId,
            Data = data
        };
        token.Serialize(ref temp);
        return new ReadOnlySpan<byte>(buffer.ToArray());
    }
}