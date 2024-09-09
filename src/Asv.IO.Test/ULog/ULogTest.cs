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
        var data = new ReadOnlySequence<byte>(TestData.ulog_log_small);

        var reader = ULog.CreateReader();

        var result = reader.TryRead<ULogTokenFileHeader>(data, out var header);
        Assert.True(result);
        Assert.NotNull(header);
        Assert.Equal(ULogToken.FileHeader,header.Type);
        Assert.Equal(20309082U, header.Timestamp);
        Assert.Equal(1,header.Version);

        result = reader.TryRead<ULogMessageFlagBits>(data, out var flag);
        Assert.True(result);
        Assert.NotNull(flag);  
        Assert.Equal(ULogToken.FlagBits,flag.Type);


    }
}