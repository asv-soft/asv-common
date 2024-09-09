using System;
using System.Buffers;
using System.Collections.Immutable;
using System.IO;
using Xunit;

namespace Asv.IO.Test;

public class ULogTest
{
    public ULogTest()
    {
        
    }
    
    [Fact]
    public void ReadHeader()
    {
        var mem = new ReadOnlySequence<byte>(TestData.ulog_log_small);
        var builder = ImmutableDictionary.CreateBuilder<byte, Func<IULogToken>>();
        builder.Add(ULogMessageFlagBits.TokenId, () => new ULogMessageFlagBits());
        var reader = new ULogReader(builder.ToImmutable(),null);

        var result = reader.TryRead(mem, out var token);
        Assert.True(result);
        Assert.NotNull(token);
        Assert.Equal(ULogToken.FileHeader,token.Type);
        var header = (ULogTokenFileHeader)token;
        Assert.Equal(20309082U, header.Timestamp);
        
    }
}