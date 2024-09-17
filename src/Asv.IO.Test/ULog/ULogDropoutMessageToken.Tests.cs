using System;
using Xunit;

namespace Asv.IO.Test;

public class ULogDropoutMessageTokenTests
{
    # region Deserialize
    
    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(ushort.MaxValue)]
    public void DeserializeToken_Success(ushort duration)
    {
        // Arrange
        var readOnlySpan = SetUpTestData(duration);
        var token = new ULogDropoutMessageToken();
        
        // Act
        token.Deserialize(ref readOnlySpan);
        
        // Assert
        Assert.Equal(duration, token.Duration);
    }
    
    # endregion
    
    # region Serialize
    
    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(ushort.MaxValue)]
    public void SerializeToken_Success(ushort duration)
    {
        // Arrange
        var readOnlySpan = SetUpTestData(duration);
        var token = SetUpTestToken(duration);
        
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
    public void GetByteSize_Success(ushort duration)
    {
        // Arrange
        var setup = SetUpTestData(duration);
        var token = SetUpTestToken(duration);
        
        // Act
        var size = token.GetByteSize();
        
        // Assert
        Assert.Equal(setup.Length, size);
    }
    
    # endregion

    #region Setup
    
    private ULogDropoutMessageToken SetUpTestToken(ushort duration)
    {
        var token = new ULogDropoutMessageToken
        {
            Duration = duration
        };

        return token;
    }
    
    private ReadOnlySpan<byte> SetUpTestData(ushort duration)
    {
        var buffer = new Span<byte>(new byte[sizeof(ushort)]);
        var temp = buffer;
        BitConverter.GetBytes(duration).CopyTo(temp);
    
        var byteArray = buffer.ToArray();
        var readOnlySpan = new ReadOnlySpan<byte>(byteArray);

        return readOnlySpan;
    }
    
    #endregion
}