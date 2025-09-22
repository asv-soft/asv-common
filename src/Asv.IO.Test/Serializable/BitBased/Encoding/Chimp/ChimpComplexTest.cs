using System;
using DotNext;
using DotNext.Buffers;
using DotNext.IO;
using Xunit;
using Xunit.Abstractions;
using ZstdSharp;

namespace Asv.IO.Test.Serializable.BitBased.Encoding.Chimp;

public class ChimpComplexTest(ITestOutputHelper log)
{
    private void RunChimpRoundtrip(ulong[] testArray, string? label = null)
    {
        var count = testArray.Length;

        var wrtStream = new PoolingArrayBufferWriter<byte>();
        using (var wrtEncoder = new ChimpEncoder(new StreamBitWriter(wrtStream.AsStream(), true)))
        {
            foreach (var t in testArray)
            {
                wrtEncoder.Add(t);
            }
        }

        using (var rdrDecoder = new ChimpDecoder(new MemoryBitReader(wrtStream.WrittenMemory)))
        {
            for (int i = 0; i < count; i++)
            {
                var v = rdrDecoder.ReadNext();
                try
                {
                    Assert.Equal(testArray[i], v);
                }
                catch
                {
                    var prev = i > 0 ? testArray[i - 1].ToString() : "N/A";
                    log.WriteLine(
                        $"Error at index {i}, prev={prev}, expected={testArray[i]}, got={v}"
                    );
                    throw;
                }
            }
        }

        using var compressor = new Compressor(100);
        var compressed = compressor.Wrap(wrtStream.WrittenMemory.Span);

        if (!string.IsNullOrEmpty(label))
        {
            log.WriteLine(label);
        }

        var rawSize = count * sizeof(ulong);
        var used = wrtStream.WrittenCount;
        var avgBytes = used / (double)count;
        var avgRatio = used / (double)rawSize;
        var compressedSize = compressed.Length;
        var compressionRatio = used / (double)compressedSize;
        var compressedToRaw = compressedSize / (double)rawSize;

        // Markdown-таблица
        log.WriteLine($"| Metric                | Value                               |");
        log.WriteLine($"|-----------------------|-------------------------------------|");
        log.WriteLine($"| Count                 | {count, -20:N0} values         |");
        log.WriteLine($"| Raw size              | {rawSize, -20:N0} bytes          |");
        log.WriteLine($"| Encoded size          | {used, -20:N0} bytes          |");
        log.WriteLine($"| Avg per value         | {avgBytes, -20:N2} bytes          |");
        log.WriteLine($"| % of raw (encoded)    | {avgRatio, -20:P2}                |");
        log.WriteLine($"| Compressed size (Zstd)| {compressedSize, -20:N0} bytes          |");
        log.WriteLine($"| Ratio enc→comp        | {compressionRatio, -20:N2}                |");
        log.WriteLine($"| % of raw (compressed) | {compressedToRaw, -20:P5}                |");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1_000)]
    [InlineData(10_000)]
    [InlineData(100_000)]
    [InlineData(1_000_000)]
    public void ChimpComplex_SerializeDeserializeConst_Works(int count)
    {
        var testArray = new ulong[count];
        var seed = Random.Shared.Next<ulong>();
        for (var i = 0; i < testArray.Length; i++)
        {
            testArray[i] = seed;
        }

        RunChimpRoundtrip(testArray, $"Const value: {seed}");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1_000)]
    [InlineData(10_000)]
    [InlineData(100_000)]
    [InlineData(1_000_000)]
    public void ChimpComplex_SerializeDeserializeRandom_Works(int count)
    {
        var testArray = new ulong[count];
        var seed = Random.Shared.Next();
        var random = new Random(seed);
        for (var i = 0; i < testArray.Length; i++)
        {
            testArray[i] = random.Next<ulong>();
        }

        RunChimpRoundtrip(testArray, $"Seed: {seed}");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1_000)]
    [InlineData(10_000)]
    [InlineData(100_000)]
    [InlineData(1_000_000)]
    public void ChimpComplex_SerializeDeserializeSequence_Works(int count)
    {
        var testArray = new ulong[count];
        for (var i = 0; i < testArray.Length; i++)
        {
            testArray[i] = (ulong)i;
        }

        RunChimpRoundtrip(testArray);
    }
}
