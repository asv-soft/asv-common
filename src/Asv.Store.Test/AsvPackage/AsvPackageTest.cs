using System.IO.Packaging;

namespace Asv.Store.Test;

public class AsvPackageTest
{
    private const string ContentType = "application/vnd.asv.test";
    private const int Version = 1;

    [Fact]
    public void Dispose_ClosesPackage_WhenChildDisposeThrows()
    {
        var filePath = CreateTempPackagePath();
        try
        {
            var package = CreatePackage(filePath);
            var sut = new TestAsvPackage(package);
            sut.AddChild(new ThrowingDisposePart(sut.ContextForTests));

            Assert.Throws<InvalidOperationException>(() => sut.Dispose());

            using var stream = File.Open(
                filePath,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None
            );
            Assert.True(stream.CanRead);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task DisposeAsync_ClosesPackage_WhenChildDisposeAsyncThrows()
    {
        var filePath = CreateTempPackagePath();
        try
        {
            var package = CreatePackage(filePath);
            var sut = new TestAsvPackage(package);
            sut.AddChild(new ThrowingDisposeAsyncPart(sut.ContextForTests));

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await sut.DisposeAsync()
            );

            using var stream = File.Open(
                filePath,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None
            );
            Assert.True(stream.CanRead);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string CreateTempPackagePath()
    {
        return Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.asv-package-test");
    }

    private static Package CreatePackage(string filePath)
    {
        var package = Package.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite);
        package.PackageProperties.ContentType = ContentType;
        package.PackageProperties.Version = Version.ToString();
        return package;
    }

    private sealed class TestAsvPackage(Package package)
        : AsvPackage(package, AsvPackageTest.Version, ContentType, logger: null)
    {
        private readonly List<AsvPackagePart> _children = [];

        public AsvPackageContext ContextForTests => Context;

        public void AddChild(AsvPackagePart child)
        {
            _children.Add(child);
        }

        public override IEnumerable<AsvPackagePart> GetChildren()
        {
            return _children;
        }
    }

    private sealed class ThrowingDisposePart(AsvPackageContext context)
        : AsvPackagePart(context, parent: null)
    {
        public override IEnumerable<AsvPackagePart> GetChildren()
        {
            return [];
        }

        public override void InternalFlush()
        {
            // do nothing
        }

        protected override void Dispose(bool disposing)
        {
            throw new InvalidOperationException("Dispose failed");
        }
    }

    private sealed class ThrowingDisposeAsyncPart(AsvPackageContext context)
        : AsvPackagePart(context, parent: null)
    {
        public override IEnumerable<AsvPackagePart> GetChildren()
        {
            return [];
        }

        public override void InternalFlush()
        {
            // do nothing
        }

        protected override ValueTask DisposeAsyncCore()
        {
            throw new InvalidOperationException("DisposeAsync failed");
        }
    }
}
