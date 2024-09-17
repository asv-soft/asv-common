using System;
using System.Buffers;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test;

public class ULogLoggedStringMessageTokenTests
{
    private readonly ITestOutputHelper _output;
    
    public ULogLoggedStringMessageTokenTests(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Fact]
    public void Calculate_ulog_file_statistic()
    {
        var data = new ReadOnlySequence<byte>(TestData.ulog_log_small);
        var rdr = new SequenceReader<byte>(data); 
        var reader = ULog.CreateReader();
        int index = 0;
        var stat = Enum.GetValues<ULogToken>().ToDictionary(token => token, token => 0);
        while (reader.TryRead(ref rdr, out var token))
        {
            Assert.NotNull(token);
            stat[token.TokenType] += 1;
            index++;
        } 
        _output.WriteLine($"Read {index} tokens");
        foreach (var (key, value) in stat)
        {
            _output.WriteLine($"{key} : {value}");
        }
    }
    
    
    [Fact]
    public void Read_ulog_file_header_and_flag_token_with_check_values()
    {
        var data = new ReadOnlySequence<byte>(TestData.ulog_log_small);
        var rdr = new SequenceReader<byte>(data); 
        var reader = ULog.CreateReader();
    
        var result = reader.TryRead<ULogFileHeaderToken>(ref rdr, out var header);
        Assert.True(result);
        Assert.NotNull(header);
        Assert.Equal(ULogToken.FileHeader,header.TokenType);
        Assert.Equal(20309082U, header.Timestamp);
        Assert.Equal(1,header.Version);
    
        result = reader.TryRead<ULogFlagBitsMessageToken>(ref rdr, out var flag);
        Assert.True(result);
        Assert.NotNull(flag);  
        Assert.Equal(ULogToken.FlagBits,flag.TokenType);
    }
}