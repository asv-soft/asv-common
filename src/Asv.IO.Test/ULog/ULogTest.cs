using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test;

public class ULogTests
{
    private readonly ITestOutputHelper _output;

    public ULogTests(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Fact]
    public void ReadHeaderWithParams()
    {
        var data = new ReadOnlySequence<byte>(TestData.ulog_log_small);
        var rdr = new SequenceReader<byte>(data); 
        var reader = ULog.CreateReader();
        var result = reader.TryRead<ULogFileHeaderToken>(ref rdr, out var header);
        Assert.True(result);
        Assert.NotNull(header);
        Assert.Equal(ULogToken.FileHeader,header.Type);
        Assert.Equal(20309082U, header.Timestamp);
        Assert.Equal(1,header.Version);
        result = reader.TryRead<ULogFlagBitsMessageToken>(ref rdr, out var flag);
        Assert.True(result);
        Assert.NotNull(flag);  
        Assert.Equal(ULogToken.FlagBits,flag.Type);

        
    }
}