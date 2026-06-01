using System.IO.Packaging;
using System.Text;
using Asv.IO;
using Asv.XUnit;
using DotNext;
using JetBrains.Annotations;

namespace Asv.Store.Test;

[TestSubject(typeof(JsonArrayAsvPackagePart<>))]
public class JsonArrayAsvPackagePartTest(ITestOutputHelper log)
{
    private static readonly Uri PartUri = new("/tables/data.jsonl", UriKind.Relative);
    private const string ContentType = "application/jsonl";
    private const string AllowedCharsName =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_ .";
    private const int NameSize = 32;

    [Theory]
    [InlineData(0, CompressionOption.NotCompressed)]
    [InlineData(1, CompressionOption.NotCompressed)]
    [InlineData(10, CompressionOption.NotCompressed)]
    [InlineData(100, CompressionOption.NotCompressed)]
    [InlineData(1000, CompressionOption.NotCompressed)]
    [InlineData(1000, CompressionOption.SuperFast)]
    [InlineData(1000, CompressionOption.Fast)]
    [InlineData(1000, CompressionOption.Normal)]
    [InlineData(1000, CompressionOption.Maximum)]
    public async Task WriteRead_Roundtrip_Works(int count, CompressionOption compression)
    {
        using var ms = new MemoryStream();
        var logger = new TestLogger(log, TimeProvider.System, "JsonArrayAsvPackagePartTest");

        var data = Enumerable
            .Range(0, count)
            .Select(i => new TestRow(
                i,
                Random.Shared.NextString(AllowedCharsName, NameSize),
                i % 2 == 0
            ))
            .ToArray();

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonArrayAsvPackagePart<TestRow>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType,
                compression: compression
            );

            await part.Write(data, CancellationToken.None);
            part.Dispose();
        }

        log.WriteLine($"Saved {count} rows, package size: {ms.Length:N} bytes");

        ms.Position = 0;
        using (var pkg = Package.Open(ms, FileMode.Open, FileAccess.Read))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonArrayAsvPackagePart<TestRow>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType
            );

            var actual = new List<TestRow>();
            await part.Read(actual.Add, CancellationToken.None);

            Assert.Equal(data, actual);
        }
    }

    [Fact]
    public async Task Read_LegacyJsonlPart_Works()
    {
        using var ms = new MemoryStream();
        var logger = new TestLogger(log, TimeProvider.System, "JsonArrayAsvPackagePartTest");
        var expected = new[]
        {
            new TestRow(1, "legacy-alpha", true),
            new TestRow(2, "legacy-beta", false),
        };

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var part = pkg.CreatePart(PartUri, ContentType, CompressionOption.NotCompressed);
            await using var stream = part.GetStream(FileMode.Create, FileAccess.Write);
            await using var writer = new StreamWriter(stream, Encoding.UTF8);

            await writer.WriteAsync(
                "/*|============================================================================ |*/\n"
            );
            await writer.WriteAsync(
                "/*| This file contains table data in JSONL format. Do not edit it manually.     |*/\n"
            );
            await writer.WriteAsync(
                "/*|============================================================================ |*/\n"
            );
            await writer.WriteAsync(
                "/*0000*/{\"Id\":1,\"Name\":\"legacy-alpha\",\"IsActive\":true}/*0000*/\n"
            );
            await writer.WriteAsync(
                "/*0001*/{\"Id\":2,\"Name\":\"legacy-beta\",\"IsActive\":false}/*0001*/\n"
            );
        }

        ms.Position = 0;
        using var readPackage = Package.Open(ms, FileMode.Open, FileAccess.Read);
        var ctx = new AsvPackageContext(new Lock(), readPackage, logger);
        var arrayPart = new JsonArrayAsvPackagePart<TestRow>(
            PartUri,
            ctx,
            parent: null,
            contentType: ContentType
        );

        var actual = new List<TestRow>();
        await arrayPart.Read(actual.Add, CancellationToken.None);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task Write_Twice_OverwritesPart()
    {
        using var ms = new MemoryStream();
        var logger = new TestLogger(log, TimeProvider.System, "JsonArrayAsvPackagePartTest");

        var first = new[] { new TestRow(1, "first", true), new TestRow(2, "second", false) };
        var second = new[] { new TestRow(10, "override", false) };

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonArrayAsvPackagePart<TestRow>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType
            );

            await part.Write(first, CancellationToken.None);
            await part.Write(second, CancellationToken.None);
            part.Dispose();
        }

        ms.Position = 0;
        using (var pkg = Package.Open(ms, FileMode.Open, FileAccess.Read))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonArrayAsvPackagePart<TestRow>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType
            );

            var actual = new List<TestRow>();
            await part.Read(actual.Add, CancellationToken.None);

            Assert.Equal(second, actual);
        }
    }

    [Fact]
    public async Task Write_CreatesExpectedJsonlStructure()
    {
        using var ms = new MemoryStream();
        var logger = new TestLogger(log, TimeProvider.System, "JsonArrayAsvPackagePartTest");
        var data = new[] { new TestRow(1, "alpha", true), new TestRow(2, "beta", false) };

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonArrayAsvPackagePart<TestRow>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType
            );

            await part.Write(data, CancellationToken.None);
            part.Dispose();
        }

        ms.Position = 0;
        using var pkgForRead = Package.Open(ms, FileMode.Open, FileAccess.Read);
        using var stream = pkgForRead.GetPart(PartUri).GetStream(FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var raw = await reader.ReadToEndAsync(CancellationToken.None);

        Assert.Contains("This file contains table data in JSONL format", raw);
        Assert.Contains("0000", raw);
        Assert.Contains("0001", raw);
        Assert.Contains("\"Name\": \"alpha\"", raw);
        Assert.Contains("\"Name\": \"beta\"", raw);
    }

    [Fact]
    public async Task Read_OneHundredThousandRows_Works()
    {
        const int count = 100_000;
        using var ms = new MemoryStream();
        var logger = new TestLogger(log, TimeProvider.System, "JsonArrayAsvPackagePartTest");
        var data = Enumerable
            .Range(0, count)
            .Select(i => new TestRow(i, $"row-{i}", i % 2 == 0))
            .ToArray();

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonArrayAsvPackagePart<TestRow>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType,
                compression: CompressionOption.Maximum
            );

            await part.Write(data, CancellationToken.None);
            part.Dispose();
        }

        ms.Position = 0;
        using var readPackage = Package.Open(ms, FileMode.Open, FileAccess.Read);
        var readContext = new AsvPackageContext(new Lock(), readPackage, logger);
        var readPart = new JsonArrayAsvPackagePart<TestRow>(
            PartUri,
            readContext,
            parent: null,
            contentType: ContentType
        );
        var readCount = 0;

        await readPart.Read(
            row =>
            {
                Assert.Equal(data[readCount], row);
                readCount++;
            },
            CancellationToken.None
        );

        Assert.Equal(count, readCount);
    }

    [Fact]
    public async Task Read_TaskNotAwaitedAndPackageDisposedDuringRead_RaisesUnobservedTaskException()
    {
        using var ms = new MemoryStream();
        var logger = new TestLogger(log, TimeProvider.System, "JsonArrayAsvPackagePartTest");
        var data = Enumerable
            .Range(0, 4)
            .Select(i => new TestRow(i, new string('x', 1024 * 1024), i % 2 == 0))
            .ToArray();

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonArrayAsvPackagePart<TestRow>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType,
                compression: CompressionOption.Maximum
            );

            await part.Write(data, CancellationToken.None);
            part.Dispose();
        }

        ms.Position = 0;
        var firstRowVisited = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var releaseVisitor = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var unobserved = new TaskCompletionSource<AggregateException>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        void Handler(object? sender, UnobservedTaskExceptionEventArgs args)
        {
            if (ContainsObjectDisposedException(args.Exception))
            {
                args.SetObserved();
                unobserved.TrySetResult(args.Exception);
            }
        }

        TaskScheduler.UnobservedTaskException += Handler;
        try
        {
            var readPackage = Package.Open(ms, FileMode.Open, FileAccess.Read);
            var ctx = new AsvPackageContext(new Lock(), readPackage, logger);
            var part = new JsonArrayAsvPackagePart<TestRow>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType
            );

            var read = StartUnobservedRead(part, firstRowVisited, releaseVisitor);

            await firstRowVisited.Task.WaitAsync(TimeSpan.FromSeconds(5));
            readPackage.Close();
            releaseVisitor.SetResult();

            await read.Completed.WaitAsync(TimeSpan.FromSeconds(5));
            await WaitForUnobservedTaskException(read.TaskReference, unobserved);

            var exception = await unobserved.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Contains(
                exception.Flatten().InnerExceptions,
                ex => ex is ObjectDisposedException
            );
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= Handler;
        }
    }

    private static (WeakReference TaskReference, Task Completed) StartUnobservedRead(
        IArrayPart<TestRow> part,
        TaskCompletionSource firstRowVisited,
        TaskCompletionSource releaseVisitor
    )
    {
        var completed = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var task = Task.Run(async () =>
        {
            await part.Read(
                _ =>
                {
                    firstRowVisited.TrySetResult();
                    releaseVisitor.Task.GetAwaiter().GetResult();
                },
                CancellationToken.None
            );
        });
        var weakReference = new WeakReference(task);
        _ = task.ContinueWith(
            _ => completed.TrySetResult(),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );
        return (weakReference, completed.Task);
    }

    private static async Task WaitForUnobservedTaskException(
        WeakReference taskReference,
        TaskCompletionSource<AggregateException> unobserved
    )
    {
        for (var i = 0; i < 20 && !unobserved.Task.IsCompleted; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (!taskReference.IsAlive)
            {
                await Task.Yield();
            }

            await Task.Delay(50);
        }
    }

    private static bool ContainsObjectDisposedException(AggregateException exception)
    {
        return exception.Flatten().InnerExceptions.Any(ex => ex is ObjectDisposedException);
    }

    public sealed record TestRow(int Id, string Name, bool IsActive);
}
