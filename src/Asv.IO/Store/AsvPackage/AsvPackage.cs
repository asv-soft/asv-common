using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Packaging;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using R3;

namespace Asv.IO;

public class AsvPackage : AsyncDisposableOnce
{
    #region Static

    public static T Open<T>(
        string filePath,
        in string contentType,
        Func<Package, int, ILogger, T> factory,
        ILogger? logger = null,
        FileAccess fileAccess = FileAccess.Read
    )
    {
        var package = Package.Open(filePath, FileMode.Open, fileAccess);
        ReadAndCheckMetadata(package, contentType, out var version);
        return factory(package, version, logger ?? NullLogger.Instance);
    }

    public static T Create<T>(
        string filePath,
        in string contentType,
        in int version,
        Func<Package, int, ILogger, T> factory,
        ILogger? logger = null
    )
    {
        var package = Package.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite);
        WriteMetadata(package, contentType, version);
        return factory(package, version, logger ?? NullLogger.Instance);
    }

    private static void WriteMetadata(Package package, in string contentType, in int version)
    {
        if (package.PackageProperties.ContentType != contentType)
        {
            package.PackageProperties.ContentType = contentType;
        }
        package.PackageProperties.Version = version.ToString();
    }

    private static void ReadAndCheckMetadata(
        Package package,
        in string contentType,
        out int version
    )
    {
        if (package.PackageProperties.ContentType != contentType)
        {
            throw new InvalidOperationException($"Package content type must be {contentType}");
        }
        if (string.IsNullOrEmpty(package.PackageProperties.Version))
        {
            throw new InvalidOperationException($"Package version is missing");
        }
        if (!int.TryParse(package.PackageProperties.Version, out version))
        {
            throw new InvalidOperationException(
                $"Package version is invalid: {package.PackageProperties.Version}"
            );
        }
    }

    #endregion

    protected ConcurrentBag<AsvPackagePart> Parts { get; } = [];
    private DisposableBag _disposeBag;

    protected AsvPackage(Package package, int version, string contentType, ILogger? logger)
    {
        Context = new AsvPackageContext(new Lock(), package, logger ?? NullLogger.Instance);
        AddToDispose(Context);
        ArgumentNullException.ThrowIfNull(package);
        Version = version;

        if (package.PackageProperties.ContentType != contentType)
        {
            throw new InvalidOperationException($"Package content type must be {contentType}");
        }
        if (package.PackageProperties.Version != version.ToString())
        {
            throw new InvalidOperationException($"Package version must be {version}");
        }
    }

    public int Version { get; }
    protected AsvPackageContext Context { get; }

    public virtual void Flush()
    {
        foreach (var part in Parts)
        {
            part.Flush();
        }
    }

    protected virtual T AddPart<T>(T part)
        where T : AsvPackagePart
    {
        Parts.Add(AddToDispose(part));
        return part;
    }

    protected T AddToDispose<T>(T obj)
    {
        if (obj is IDisposable disposable)
        {
            _disposeBag.Add(disposable);
        }
        return obj;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposeBag.Dispose();
            ((IDisposable)Context.Package).Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await CastAndDispose(_disposeBag);
        await CastAndDispose(Context.Package);

        await base.DisposeAsyncCore();

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync();
            }
            else
            {
                resource.Dispose();
            }
        }
    }
}
