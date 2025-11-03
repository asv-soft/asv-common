using System;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Threading;
using Asv.Cfg.Test;
using Asv.IO;
using DeepEqual.Syntax;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test;

[TestSubject(typeof(VisitableTimeSeriesAsvPackagePart))]
public class VisitableTimeSeriesAsvPackagePartTest(ITestOutputHelper log)
{
    private static readonly Uri PartUri = new Uri("/meta/ts.json", UriKind.Relative);
    private const string ContentType = "application/json";

    private void Test(
        SupportedTypesWithArraysAndSubs[] array,
        uint flushEvery,
        CompressionOption compressionOption,
        bool useZstdForBatch
    )
    {
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
        for (var i = 0; i < array.Length; i++)
        {
            part.Write(new TableRow((uint)i, start.AddSeconds(i), "test", array[i]));
            size += SimpleBinaryMixin.GetSize(array[i]);
        }

        sw.Stop();
        log.WriteLine(
            $"Write {array.Length} records with flushEvery={flushEvery} in {sw.ElapsedMilliseconds} ms"
        );
        part.Flush();
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
                rec.Item1.Data.ShouldDeepEqual(array[r.Index]);
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
