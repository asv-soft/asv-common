# Packages and ZIP archives

## JsonPackageConfiguration

This backend writes all keys into one JSON part of an existing `System.IO.Packaging.Package`. It derives from `JsonConfigurationBase`, so it uses an in-memory case-insensitive dictionary and supports the deferred-save and sorting options described in [JSON backends](Cfg-Json.md).

```csharp
using System.IO.Packaging;
using Asv.Cfg;

using var stream = new MemoryStream();
using var package = Package.Open(stream, FileMode.Create, FileAccess.ReadWrite);
var partUri = new Uri("/configuration.json", UriKind.Relative);
var part = package.CreatePart(partUri, "application/json", CompressionOption.Normal);
using (var writer = new StreamWriter(part.GetStream(FileMode.Create, FileAccess.Write)))
{
    writer.Write("{\"RetryCount\":3}");
}
using var configuration = new JsonPackageConfiguration(
    package: package,
    partUri: partUri,
    contentType: "application/json",
    compression: CompressionOption.Normal,
    packageLock: null);
Console.WriteLine(configuration.Get("RetryCount", 0));
```

The existing part must have the requested content type. The example prepares an existing JSON part and reads an existing key. A missing part can also be created by the backend.

### Writing

Saving replaces the part and flushes the package. Creation and writing use `FileMode.Create`, which is supported by standard `PackagePart` streams. This corrects the previous `CreateNew not supported` failure. The write is not transactional; an I/O failure during replacement can still leave a partial result.

`packageLock` accepts a shared `System.Threading.Lock`; it guards part replacement setup and package flushing, not the entire lifetime of the returned write stream or the constructor's loading path. Coordinate concurrent package access externally when necessary.

The caller owns the package. Dispose the configuration before the package. The backend does not close the caller's package. It has no reserved keys. Deferred-write and sorting options are inherited from `JsonConfigurationBase`, and have the same persistence timing limitations.

## ZipJsonConfiguration

```csharp
using Asv.Cfg;

using var stream = new MemoryStream();
using (var configuration = new ZipJsonConfiguration(stream, leaveOpen: true))
{
    configuration.Set("RetryCount", 3);
}
stream.Position = 0;
using var reopened = new ZipJsonConfiguration(stream, leaveOpen: true);
Console.WriteLine(reopened.Get("RetryCount", 0));
```

The constructor takes a stream, optional `leaveOpen` (false by default), and an optional logger. It opens `ZipArchiveMode.Update`; use a stream supporting the operations required by update mode, such as a read/write seekable file or `MemoryStream`.

Each key is stored as `<key>.json`. Saving locates the entry using `DefaultKeyComparer`, preserves its full stored name, and replaces its payload. The archive has an instance-level lock, but the whole multi-call read/update/write sequence is not atomic. Lookup, replacement, and removal use the same comparer. Conflicting entry names are rejected before modification, as described in [Keys and values](Cfg-Values.md).

Disposing the configuration disposes its archive and finalizes the ZIP structure. It closes the underlying stream unless `leaveOpen` is true. Finish disposal before reading, copying, or exporting the completed ZIP. With nested `using` declarations, dispose the configuration before its stream.

`AvailableParts` lists JSON entry names without extensions. Plain ZIP configuration has no reserved keys. A default created by `Get` is written directly into a new entry without emitting `OnChanged`.

## Choosing the format

A package part containing a whole settings object and a ZIP archive containing one entry per key are different formats. `JsonPackageConfiguration` and `ZipJsonConfiguration` are not interchangeable readers. Neither provides encryption, automatic migrations, cross-process locking, or a transaction across several configuration keys.
