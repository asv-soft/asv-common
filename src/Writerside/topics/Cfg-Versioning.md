# Versioned configuration files

`ZipJsonVersionedFile` extends `ZipJsonConfiguration` and implements `IVersionedFile`. It validates file metadata at construction; it does not automatically migrate settings.

## Create or open a file

```csharp
using Asv.Cfg;
using Asv.Common;

using var stream = new MemoryStream();
using var configuration = new ZipJsonVersionedFile(
    stream: stream,
    lastVersion: new SemVersion(1, 0, 0),
    fileType: "MyApplicationSettings",
    createIfNotExist: true,
    leaveOpen: true);
configuration.Set("RetryCount", 3);
Console.WriteLine(configuration.FileVersion);
```

`lastVersion` is the maximum version this reader accepts. `fileType` identifies the expected application format. `createIfNotExist` controls creation of missing metadata; it is not a file-system creation flag because the caller already supplies the stream.

## Metadata representation

Metadata key lookup uses `DefaultKeyComparer`, so an existing `fileinfo.json` is recognized. Conflicting metadata entries are rejected before the constructor selects a value.

The reserved key `FileInfo` is stored as `FileInfo.json` and represented by the immutable `ZipJsonFileInfo` struct:

```json
{
  "FileVersion": "1.0.0",
  "FileType": "MyApplicationSettings"
}
```

`ZipJsonFileInfo.Empty` contains two empty strings. The struct exposes value equality and equality operators. `IVersionedFile.FileVersion` returns the version accepted during construction.

## Validation sequence

1. Load metadata, using `ZipJsonFileInfo.Empty` if it is absent.
2. For empty metadata, create the requested version/type if creation is allowed; otherwise throw `ConfigurationException`.
3. For existing metadata, parse `FileVersion` with `SemVersion.TryParse` and reject an invalid version or a version newer than `lastVersion`.
4. Compare `FileType` with the expected type using `CurrentCultureIgnoreCase`; reject a mismatch.

Older versions are accepted without an upgrade. Opening an old file does not rewrite its metadata to `lastVersion`. Application code must interpret or migrate older settings explicitly.

## Important limits

`ReservedParts` includes `FileInfo`, but normal `Set` and `Remove` operations do not enforce this reservation. Treat it as backend-owned metadata. Changing it after construction does not recalculate the instance's `FileVersion` field.

Loading missing metadata uses the regular default-creation path, which can add an empty `FileInfo` entry before creation is rejected. A failed open is therefore not guaranteed to leave the supplied archive untouched. Inspect untrusted files using a disposable copy when preserving the original bytes matters.

Keep migration logic version-specific and preserve a recoverable original before rewriting a file. Rejecting a future version protects format interpretation; it is not a signature or authenticity check. ZIP finalization and stream ownership follow [Archive backends](Cfg-Archives.md).
