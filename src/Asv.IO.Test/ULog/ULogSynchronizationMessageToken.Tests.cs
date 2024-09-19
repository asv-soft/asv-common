using System;
using Asv.IO.Synchronization;
using Xunit;

namespace Asv.IO.Test;

public class ULogSynchronizationMessageTokenTests
{
    # region Deserialize
    
    [Fact]
    public void DeserializeToken_Success()
    {
        // Arrange
        var data = ULogSynchronizationMessageToken.SyncMagic;
        var readOnlySpan = new ReadOnlySpan<byte>(data);
        var token = new ULogSynchronizationMessageToken();

        // Act + Assert
        token.Deserialize(ref readOnlySpan);
    }
    
    [Fact]
    public void DeserializeToken_WrongBytes()
    {
        Assert.Throws<ULogException>(() =>
        {
            var data = new[]
            {
                (byte)(ULogSynchronizationMessageToken.SyncMagic[0] + 1), 
                (byte)(ULogSynchronizationMessageToken.SyncMagic[1] + 1), 
                (byte)(ULogSynchronizationMessageToken.SyncMagic[2] + 1), 
                (byte)(ULogSynchronizationMessageToken.SyncMagic[3] + 1), 
                (byte)(ULogSynchronizationMessageToken.SyncMagic[4] + 1), 
                (byte)(ULogSynchronizationMessageToken.SyncMagic[5] + 1), 
                (byte)(ULogSynchronizationMessageToken.SyncMagic[6] + 1)
            };
            
            var readOnlySpan = new ReadOnlySpan<byte>(data);
            var token = new ULogSynchronizationMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }
    
    [Fact]
    public void DeserializeToken_LessBytes_Error()
    {
        Assert.Throws<ULogException>(() =>
        {
            var data = new byte[ULogSynchronizationMessageToken.SyncMagic.Length - 1];
            var readOnlySpan = new ReadOnlySpan<byte>(data);
            var token = new ULogSynchronizationMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }
    
    [Fact]
    public void DeserializeToken_MoreBytes_Error()
    {
        Assert.Throws<ULogException>(() =>
        {
            var data = new byte[ULogSynchronizationMessageToken.SyncMagic.Length + 1];
            var readOnlySpan = new ReadOnlySpan<byte>(data);
            var token = new ULogSynchronizationMessageToken();
            token.Deserialize(ref readOnlySpan);
        });
    }

    # endregion

    # region Serialize

    [Fact]
    public void SerializeToken_Success()
    {
        // Arrange
        var token = SetUpTestToken();
    
        // Act
        var span = new Span<byte>(new byte[ULogSynchronizationMessageToken.SyncMagic.Length]);
        var temp = span;
        token.Serialize(ref temp);
    
        // Assert
        Assert.True(span.SequenceEqual(ULogSynchronizationMessageToken.SyncMagic));
    }
    
    [Fact]
    public void SerializeToken_NotEnoughSpace()
    {
        Assert.Throws<IndexOutOfRangeException>(() =>
        {
            var token = SetUpTestToken();
            var span = new Span<byte>(new byte[ULogSynchronizationMessageToken.SyncMagic.Length - 1]);
            var temp = span;
            token.Serialize(ref temp);
        });
    }
    
    # endregion
    
    # region GetByteSize
    
    [Fact]
    public void GetByteSize_Success()
    {
        // Arrange
        var token = SetUpTestToken();
    
        // Act
        var size = token.GetByteSize();
    
        // Assert
        Assert.Equal(ULogSynchronizationMessageToken.SyncMagic.Length, size);
    }
    
    # endregion

    #region Setup

    private ULogSynchronizationMessageToken SetUpTestToken()
    {
        var token = new ULogSynchronizationMessageToken();

        return token;
    }

    #endregion
}