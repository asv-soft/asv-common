using System;
using System.Buffers;
using System.Collections.Immutable;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test;

public class ULogTest
{
    private readonly ITestOutputHelper _output;

    public ULogTest(ITestOutputHelper output)
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
        Assert.Equal(ULogToken.FileHeader,header.Type);
        Assert.Equal(20309082U, header.Timestamp);
        Assert.Equal(1,header.Version);

        result = reader.TryRead<ULogFlagBitsMessageToken>(ref rdr, out var flag);
        Assert.True(result);
        Assert.NotNull(flag);  
        Assert.Equal(ULogToken.FlagBits,flag.Type);

        while (reader.TryRead(ref rdr, out var token))
        {
            Assert.NotNull(token);
            if (token.Type == ULogToken.Format)
            {
                var format = token as ULogFormatMessageToken;
                _output.WriteLine($"Format: {format.Type:G} {string.Join(",",format.Fields)}");    
            }
        } 

        


    }
}