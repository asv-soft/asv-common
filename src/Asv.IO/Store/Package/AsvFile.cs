﻿using System;
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

public class AsvFile : AsyncDisposableOnce
{
    #region Static

    public static T Open<T>(
        string filePath,
        in string contentType,
        Func<Package, int, ILogger, T> factory,
        ILogger? logger
    )
    {
        var package = Package.Open(filePath, FileMode.Open, FileAccess.Read);
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

    private readonly ConcurrentBag<AsvFilePart> _parts = [];
    private DisposableBag _disposeBag;

    protected AsvFile(Package package, int version, string contentType, ILogger? logger)
    {
        Context = new AsvFileContext(new Lock(), package, logger ?? NullLogger.Instance);
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
    protected AsvFileContext Context { get; }

    public void Flush()
    {
        foreach (var part in _parts)
        {
            part.Flush();
        }
    }

    protected T AddPart<T>(T part)
        where T : AsvFilePart
    {
        _parts.Add(AddToDispose(part));
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
