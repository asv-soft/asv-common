using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Threading;

namespace Asv.Cfg;

public class JsonPackageConfiguration : JsonConfigurationBase
{
    private readonly Package _package;
    private readonly Uri _partUri;
    private readonly string _contentType;
    private readonly CompressionOption _compression;
    private readonly object? _packageLock;

    public JsonPackageConfiguration(Package package, Uri partUri, string contentType, CompressionOption compression, object? packageLock, TimeSpan? flushToFileDelayMs = null, bool sortKeysInFile = false, TimeProvider? timeProvider = null) 
        : base(() => LoadCallback(package, partUri,contentType,compression), flushToFileDelayMs, sortKeysInFile, timeProvider)
    {
        ArgumentNullException.ThrowIfNull(package);
        _package = package;
        _partUri = partUri;
        _contentType = contentType;
        _compression = compression;
        _packageLock = packageLock;
    }

    private static Stream LoadCallback(Package package, Uri partUri, string contentType, CompressionOption compression)
    {
        if (package.PartExists(partUri))
        {
            var part = package.GetPart(partUri);
            if (part.ContentType != contentType)
            {
                throw new InvalidOperationException($"Part {partUri} has wrong content type: {part.ContentType}");
            }
            return part.GetStream(FileMode.Open, FileAccess.Read);
        }
        var newPart = package.CreatePart(partUri, contentType, compression);
        return newPart.GetStream(FileMode.CreateNew);
    }


    protected override IEnumerable<string> InternalSafeGetReservedParts()
    {
        return [];
    }

    protected override void EndSaveChanges()
    {
        if (_packageLock != null) Monitor.Enter(_packageLock);
        try
        {
            _package.Flush();
        }
        finally
        {
            if (_packageLock != null) Monitor.Exit(_packageLock);
        }
    }

    protected override Stream BeginSaveChanges()
    {
        if (_packageLock != null) Monitor.Enter(_packageLock);
        try
        {
            if (_package.PartExists(_partUri))
            {
                _package.DeletePart(_partUri);
            }
            var part = _package.CreatePart(_partUri, _contentType, _compression);
            return part.GetStream(FileMode.CreateNew);
        }
        finally
        {
            if (_packageLock != null) Monitor.Exit(_packageLock);
        }
    }
}