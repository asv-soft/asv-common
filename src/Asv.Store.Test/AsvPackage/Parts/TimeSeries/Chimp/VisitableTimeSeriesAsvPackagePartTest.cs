using System.Diagnostics;
using System.IO.Packaging;
using Asv.IO;
using Asv.XUnit;
using DeepEqual.Syntax;
using JetBrains.Annotations;

namespace Asv.Store.Test;

[TestSubject(typeof(VisitableTimeSeriesAsvPackagePart))]
public class VisitableTimeSeriesAsvPackagePartTest(ITestOutputHelper log)
{
    private static readonly Uri PartUri = new Uri("/meta/ts.json", UriKind.Relative);
    private const string ContentType = "application/json";

    private void Test(
        SupportedTypesWithArraysAndSubs[] array,
        uint flushEvery,
        CompressionOption compressionOption,
        bool useZstdForBatch,
        DateTime[]? timestamps = null
    )
    {
        if (timestamps is not null && timestamps.Length != array.Length)
        {
            throw new ArgumentException(
                "Timestamp count must match the record count.",
                nameof(timestamps)
            );
        }

        var ms = new MemoryStream();
        var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
        var logger = new TestLogger(log, TimeProvider.System, "AsvFilePartTest");
        var ctx = new AsvPackageContext(new Lock(), pkg, logger);
        var part = new VisitableTimeSeriesAsvPackagePart(
            PartUri,
            ContentType,
            flushEvery,
            ctx,
            compressionOption,
            useZstdForBatch
        );

        var sw = Stopwatch.StartNew();
        int size = 0;
        var start = DateTime.Now;
        var expectedTimestamps = new DateTime[array.Length];
        for (var i = 0; i < array.Length; i++)
        {
            var timestamp = timestamps?[i] ?? start.AddSeconds(i);
            expectedTimestamps[i] = timestamp;
            part.Write(new TableRow((uint)i, timestamp, "test", array[i]));
            size += SimpleBinaryMixin.GetSize(array[i]);
        }

        sw.Stop();
        log.WriteLine(
            $"Write {array.Length} records with flushEvery={flushEvery} in {sw.ElapsedMilliseconds} ms"
        );
        part.InternalFlush();
        part.Dispose();
        pkg.Flush();
        pkg.Close();

        ms.Position = 0;
        pkg = Package.Open(ms, FileMode.Open, FileAccess.Read);
        ctx = new AsvPackageContext(new Lock(), pkg, logger);
        part = new VisitableTimeSeriesAsvPackagePart(PartUri, ContentType, flushEvery, ctx);

        sw.Restart();

        var counter = 0;
        part.Read(
            rec =>
            {
                var (r, o) = rec;
                Assert.Equal("test", r.Id);
                Assert.IsType<SupportedTypesWithArraysAndSubs>(r.Data);
                var index = checked((int)r.Index);
                Assert.Equal(expectedTimestamps[index], r.Timestamp);
                rec.Item1.Data.ShouldDeepEqual(array[index]);
                counter++;
            },
            id =>
            {
                if (id == "test")
                {
                    return (new SupportedTypesWithArraysAndSubs(), new object());
                }
                return null;
            }
        );
        Assert.Equal(array.Length, counter);
        sw.Stop();
        log.WriteLine(
            $"Read {array.Length} records with flushEvery={flushEvery} in {sw.ElapsedMilliseconds} ms"
        );
        var ratio = (double)size / ms.Length;
        log.WriteLine(
            $"Size uncompressed: {size:N0} bytes, compressed: {ms.Length:N0} bytes, ratio: {ratio:0.00}"
        );

        part.Dispose();
        pkg.Close();
        ms.Dispose();
    }

    [Theory]
    [InlineData(1, 1, true, CompressionOption.Maximum)]
    [InlineData(10, 11, true, CompressionOption.Maximum)]
    [InlineData(100, 10, true, CompressionOption.Maximum)]
    [InlineData(1_000, 100, true, CompressionOption.Maximum)]
    [InlineData(4_500, 300, true, CompressionOption.Maximum)]
    [InlineData(4_500, 300, false, CompressionOption.Maximum)]
    [InlineData(4_500, 300, false, CompressionOption.NotCompressed)]
    [InlineData(10_000, 1000, true, CompressionOption.Maximum)]
    [InlineData(100_000, 1000, false, CompressionOption.NotCompressed)]
    [InlineData(100_000, 1000, true, CompressionOption.NotCompressed)]
    [InlineData(100_000, 1000, true, CompressionOption.Maximum)]
    public void ReadWriteRandomized_ShouldCorrectlyWriteAndReadRecords_WithVariousFlushIntervals(
        int count,
        uint flushEvery,
        bool useZstdForBatch,
        CompressionOption compressionOption
    )
    {
        var array = new SupportedTypesWithArraysAndSubs[count];
        for (var i = 0; i < count; i++)
        {
            array[i] = new SupportedTypesWithArraysAndSubs().Randomize();
        }

        Test(array, flushEvery, compressionOption, useZstdForBatch);
    }

    [Fact]
    public void ReadWrite_ShouldPreserveTimestamps_WhenFirstDeltaExceeds27BitRange()
    {
        var array = new[]
        {
            new SupportedTypesWithArraysAndSubs().Randomize(),
            new SupportedTypesWithArraysAndSubs().Randomize(),
        };
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var timestamps = new[] { start, start.AddHours(1) };

        Test(
            array,
            flushEvery: 10,
            compressionOption: CompressionOption.NotCompressed,
            useZstdForBatch: false,
            timestamps: timestamps
        );
    }

    [Theory]
    [InlineData(1, 1, true, CompressionOption.Maximum)]
    [InlineData(10, 10, true, CompressionOption.Maximum)]
    [InlineData(100, 10, true, CompressionOption.Maximum)]
    [InlineData(1_000, 100, true, CompressionOption.Maximum)]
    [InlineData(4_500, 300, true, CompressionOption.Maximum)]
    [InlineData(4_500, 300, false, CompressionOption.Maximum)]
    [InlineData(4_500, 300, false, CompressionOption.NotCompressed)]
    [InlineData(10_000, 1000, true, CompressionOption.Maximum)]
    [InlineData(100_000, 1000, false, CompressionOption.NotCompressed)]
    [InlineData(100_000, 1000, true, CompressionOption.NotCompressed)]
    [InlineData(100_000, 1000, true, CompressionOption.Maximum)]
    public void ReadWriteConst_ShouldCorrectlyWriteAndReadRecords_WithVariousFlushIntervals(
        int count,
        uint flushEvery,
        bool useZstdForBatch,
        CompressionOption compressionOption
    )
    {
        var array = new SupportedTypesWithArraysAndSubs[count];
        var constValue = new SupportedTypesWithArraysAndSubs().Randomize();
        for (var i = 0; i < count; i++)
        {
            array[i] = constValue;
        }

        Test(array, flushEvery, compressionOption, useZstdForBatch);
    }

    [Theory]
    [InlineData(1, 10, true, CompressionOption.Maximum)]
    [InlineData(10, 10, true, CompressionOption.Maximum)]
    [InlineData(100, 10, true, CompressionOption.Maximum)]
    [InlineData(1_000, 100, true, CompressionOption.Maximum)]
    [InlineData(4_500, 300, true, CompressionOption.Maximum)]
    [InlineData(4_500, 300, false, CompressionOption.Maximum)]
    [InlineData(4_500, 300, false, CompressionOption.NotCompressed)]
    [InlineData(10_000, 100, true, CompressionOption.Maximum)]
    [InlineData(100_000, 1000, false, CompressionOption.NotCompressed)]
    [InlineData(100_000, 1000, true, CompressionOption.NotCompressed)]
    [InlineData(100_000, 1000, true, CompressionOption.Maximum)]
    public void ReadWriteIncrement_ShouldCorrectlyWriteAndReadRecords_WithVariousFlushIntervals(
        int count,
        uint flushEvery,
        bool useZstdForBatch,
        CompressionOption compressionOption
    )
    {
        var array = new SupportedTypesWithArraysAndSubs[count];
        for (var i = 0; i < count; i++)
        {
            array[i] = new SupportedTypesWithArraysAndSubs().RandomizeIncremental(i, count);
        }

        Test(array, flushEvery, compressionOption, useZstdForBatch);
    }
}
