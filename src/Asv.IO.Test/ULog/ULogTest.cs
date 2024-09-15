using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
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

        Dictionary<string, List<Tuple<int, byte, byte, string>>> multiInfo = new();
        while (reader.TryRead(ref rdr, out var token))
        {
            Assert.NotNull(token);
            /*if (token.Type == ULogToken.Format)
            {
                var format = token as ULogFormatMessageToken;
                _output.WriteLine($"Format: {format.Type:G} {string.Join(",",format.Fields)}");    
            }*/
            if (token.Type == ULogToken.MultiInformation)
            {
                var multi = token as ULogMultiInformationMessageToken;
                try
                {
                    multiInfo[multi.KeyName].Add(new(multi.ValueLenght, multi.IsContinued, multi.KeyLenght, multi.Value));
                }
                catch (KeyNotFoundException)
                {
                    multiInfo.Add(multi.KeyName, new List<Tuple<int, byte, byte, string>>
                    {
                        new(multi.ValueLenght, multi.IsContinued, multi.KeyLenght, multi.Value)
                    });
                }
            }
            
        } 
        
    }

    [Theory]
    [InlineData(69, 0, 31, "char[36] perf_counter_preflight", "vehicle_imu: gyro data gap: 1 events")]
    [InlineData(128, 1, 31, "char[95] perf_counter_preflight", "vehicle_imu: gyro update interval: 7612 events, 2443.69 avg, min 2024us max 2545us 62.673us rms")]
    [InlineData(70, 1, 31, "char[37] perf_counter_preflight", "vehicle_imu: accel data gap: 1 events")]
    [InlineData(129, 1, 31, "char[109] perf_counter_preflight", "vehicle_imu: vehicle_imu: accel update interval: 7612 events, 2443.69 avg, min 2024us max 2545us 62.673us rms")]
    [InlineData(69, 1, 31, "char[36] perf_counter_preflight", "vehicle_imu: gyro data gap: 1 events")]    
    [InlineData(128, 1, 31, "char[95] perf_counter_preflight", "vehicle_imu: gyro update interval: 7442 events, 2499.66 avg, min 2364us max 2888us 53.563us rms")]
    public void WriteMultiInformation(int size, byte isContinued, byte keyLenght, string key, string value)
    {
        var token = new ULogMultiInformationMessageToken
        {
            IsContinued = isContinued,
            KeyLenght = keyLenght,
            Key = key,
            Value = value
        };

        var array = ArrayPool<byte>.Shared.Rent(size);
        var buffer = new Span<byte>(array, 0, size);
        
        token.Serialize(ref buffer);
    }

    [Theory]
    [InlineData(new byte[] {0, 31, 99, 104, 97, 114, 91, 51, 54, 93, 32, 112, 101, 114, 102, 95, 99, 111, 117, 110, 116, 101, 114, 95, 112, 114, 101, 102, 108, 105, 103, 104, 116, 118, 101, 104, 105, 99, 108, 101, 95, 105, 109, 117, 58, 32, 103, 121, 114, 111, 32, 100, 97, 116, 97, 32, 103, 97, 112, 58, 32, 49, 32, 101, 118, 101, 110, 116, 115},
        0, 31, "char[36] perf_counter_preflight", "vehicle_imu: gyro data gap: 1 events")]
    [InlineData(new byte[] {1, 31, 99, 104, 97, 114, 91, 57, 53, 93, 32, 112, 101, 114, 102, 95, 99, 111, 117, 110, 116, 101, 114, 95, 112, 114, 101, 102, 108, 105, 103, 104, 116, 118, 101, 104, 105, 99, 108, 101, 95, 105, 109, 117, 58, 32, 103, 121, 114, 111, 32, 117, 112, 100, 97, 116, 101, 32, 105, 110, 116, 101, 114, 118, 97, 108, 58, 32, 55, 54, 49, 50, 32, 101, 118, 101, 110, 116, 115, 44, 32, 50, 52, 52, 51, 46, 54, 57, 32, 97, 118, 103, 44, 32, 109, 105, 110, 32, 50, 48, 50, 52, 117, 115, 32, 109, 97, 120, 32, 50, 53, 52, 53, 117, 115, 32, 54, 50, 46, 54, 55, 51, 117, 115, 32, 114, 109, 115},
        1, 31, "char[95] perf_counter_preflight", "vehicle_imu: gyro update interval: 7612 events, 2443.69 avg, min 2024us max 2545us 62.673us rms")]
    public void ReadMultiInformation(byte[] rawData, byte expectedIsContinued, byte expectedLenght, string expectedKey, string expectedValue)
    {
        var data = new ReadOnlySpan<byte>(rawData);
        var token = new ULogMultiInformationMessageToken();

        token.Deserialize(ref data);
        Assert.NotNull(token);
        Assert.Equal(expectedIsContinued, token.IsContinued);
        Assert.Equal(expectedLenght, token.KeyLenght);
        Assert.Equal(expectedKey, token.Key);
        Assert.Equal(expectedValue, token.Value);
    }
}
