using System;
using DotNext.Buffers;
using DotNext.IO;
using Xunit;
using Xunit.Abstractions;
using ZstdSharp;

namespace Asv.IO.Test.Serializable.BitBased.Encoding.Gorilla;

public class GorillaTimestampComplexTest(ITestOutputHelper log)
{
    private void RunGorillaRoundtrip(
        long[] testArray,
        bool firstDelta27Bits = true,
        string? label = null
    )
    {
        var count = testArray.Length;

        var wrtStream = new PoolingArrayBufferWriter<byte>();
        using (
            var wrtEncoder = new GorillaTimestampEncoder(
                new StreamBitWriter(wrtStream.AsStream()),
                firstDelta27Bits: firstDelta27Bits
            )
        )
        {
            foreach (var t in testArray)
            {
                wrtEncoder.Add(t);
            }
        }

        using (
            var rdrDecoder = new GorillaTimestampDecoder(
                new MemoryBitReader(wrtStream.WrittenMemory),
                firstDelta27Bits: firstDelta27Bits
            )
        )
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

        var rawSize = count * sizeof(long);
        var used = wrtStream.WrittenCount;
        var avgBytes = count > 0 ? used / (double)count : 0;
        var avgRatio = rawSize > 0 ? used / (double)rawSize : 0;
        var compressedSize = compressed.Length;
        var compressionRatio = compressedSize > 0 ? used / (double)compressedSize : 0;
        var compressedToRaw = rawSize > 0 ? compressedSize / (double)rawSize : 0;

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

    private static long[] BuildSequence(int count)
    {
        var arr = new long[count];
        for (int i = 0; i < count; i++)
        {
            arr[i] = i;
        }

        return arr;
    }

    /// <summary>
    /// Монотонные таймштампы: t[i] = t[i-1] + Δ, где Δ ~ [0..maxStep].
    /// Это соответствует модели Gorilla (малый DoD).
    /// </summary>
    private static long[] BuildMonotonicRandom(int count, int maxStep = 1_000)
    {
        var arr = new long[count];
        if (count == 0)
        {
            return arr;
        }

        var seed = Random.Shared.Next();
        var rnd = new Random(seed);

        // База (эмулируем «эпоху + случайный сдвиг»), чтобы не упереться в переполнение
        long t = 1_600_000_000_000L + rnd.Next(0, 1_000_000); // например, мс
        arr[0] = t;

        for (int i = 1; i < count; i++)
        {
            // шаг неотрицательный и умеренный
            var step = rnd.Next(0, maxStep + 1);
            t += step;
            arr[i] = t;
        }

        return arr;
    }

    private static long[] BuildConst(int count)
    {
        var arr = new long[count];
        if (count == 0)
        {
            return arr;
        }

        var seed = Random.Shared.NextInt64();
        for (int i = 0; i < count; i++)
        {
            arr[i] = seed;
        }

        return arr;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1_000)]
    [InlineData(10_000)]
    [InlineData(100_000)]
    [InlineData(1_000_000)]
    public void GorillaTimestamp_SerializeDeserialize_Sequence_Works(int count) =>
        RunGorillaRoundtrip(BuildSequence(count), firstDelta27Bits: true, label: "Sequence");

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1_000)]
    [InlineData(10_000)]
    [InlineData(100_000)]
    [InlineData(1_000_000)]
    public void GorillaTimestamp_SerializeDeserialize_Const_Works(int count) =>
        RunGorillaRoundtrip(BuildConst(count), firstDelta27Bits: true, label: "Const");

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1_000)]
    [InlineData(10_000)]
    [InlineData(100_000)]
    [InlineData(1_000_000)]
    public void GorillaTimestamp_SerializeDeserialize_RandomMonotonic_Works(int count) =>
        RunGorillaRoundtrip(
            BuildMonotonicRandom(count, maxStep: 1_000),
            firstDelta27Bits: true,
            label: "Random monotonic (Δ≤1000)"
        );
}
