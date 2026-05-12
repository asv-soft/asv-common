using System.IO.Packaging;
using System.Text;
using Asv.XUnit;
using JetBrains.Annotations;

namespace Asv.Store.Test;

[TestSubject(typeof(ArrayAsvPackagePart<>))]
public class ArrayAsvPackagePartAsyncLifecycleTest(ITestOutputHelper log)
{
    private static readonly Uri PartUri = new("/tables/async.txt", UriKind.Relative);
    private const string ContentType = "text/plain";

    [Fact]
    public async Task Read_AwaitsInternalReadBeforeDisposingStream()
    {
        using var ms = new MemoryStream();
        var logger = new TestLogger(
            log,
            TimeProvider.System,
            nameof(ArrayAsvPackagePartAsyncLifecycleTest)
        );

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var part = pkg.CreatePart(PartUri, ContentType, CompressionOption.NotCompressed);
            await using var stream = part.GetStream(FileMode.Create, FileAccess.Write);
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync("42");
        }

        ms.Position = 0;
        using var readPackage = Package.Open(ms, FileMode.Open, FileAccess.Read);
        var ctx = new AsvPackageContext(new Lock(), readPackage, logger);
        var arrayPart = new AsyncArrayPart(PartUri, ctx);

        var values = new List<string>();
        await arrayPart.Read(values.Add, CancellationToken.None);

        Assert.Equal(["42"], values);
        Assert.False(arrayPart.StreamWasDisposedBeforeReadCompleted);
    }

    [Fact]
    public async Task Write_AwaitsInternalWriteBeforeDisposingStream()
    {
        using var ms = new MemoryStream();
        var logger = new TestLogger(
            log,
            TimeProvider.System,
            nameof(ArrayAsvPackagePartAsyncLifecycleTest)
        );

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var arrayPart = new AsyncArrayPart(PartUri, ctx);

            await arrayPart.Write(["alpha"], CancellationToken.None);

            Assert.False(arrayPart.StreamWasDisposedBeforeWriteCompleted);
        }

        ms.Position = 0;
        using var readPackage = Package.Open(ms, FileMode.Open, FileAccess.Read);
        using var stream = readPackage.GetPart(PartUri).GetStream(FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(stream);

        Assert.Equal("alpha", await reader.ReadToEndAsync(CancellationToken.None));
    }

    private sealed class AsyncArrayPart(Uri path, AsvPackageContext context)
        : ArrayAsvPackagePart<string>(
            path,
            context,
            parent: null,
            contentType: ContentType,
            compressionOption: CompressionOption.NotCompressed
        )
    {
        public bool StreamWasDisposedBeforeReadCompleted { get; private set; }
        public bool StreamWasDisposedBeforeWriteCompleted { get; private set; }

        protected override async ValueTask InternalRead(
            Stream stream,
            Action<string> visitor,
            CancellationToken cancel
        )
        {
            await Task.Yield();
            StreamWasDisposedBeforeReadCompleted = !stream.CanRead;

            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            visitor(await reader.ReadToEndAsync(cancel));
        }

        protected override async ValueTask InternalWrite(
            Stream stream,
            IEnumerable<string> values,
            CancellationToken cancel
        )
        {
            await Task.Yield();
            StreamWasDisposedBeforeWriteCompleted = !stream.CanWrite;

            await using var writer = new StreamWriter(stream, leaveOpen: true);
            foreach (var value in values)
            {
                await writer.WriteAsync(value.AsMemory(), cancel);
            }
        }
    }
}
