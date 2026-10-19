# In-memory and JSON file backends

## InMemoryConfiguration

Construct with an optional logger. Values are converted to JSON tokens on `Set` and materialized on `Get`; they are not a live object graph. Keys use `ConfigurationMixin.DefaultKeyComparer`, ignoring case. There are no reserved keys or disk writes. Disposal clears the store.

It is suitable for isolated tests, but its default serializer differs from persistent JSON dictionaries. Test persistence-sensitive behavior against the intended backend too.

## JsonConfiguration: a folder of files

```csharp
using Asv.Cfg;

using var configuration = new JsonConfiguration("settings");
configuration.Set("RetryCount", 3);
Console.WriteLine(configuration.Get("RetryCount", 0));
Console.WriteLine(configuration.WorkingFolder);
```

The constructor accepts `folderPath`, optional `ILogger`, and optional `IFileSystem`. It creates a missing directory and exposes its absolute path through `WorkingFolder`. Each new key becomes `<key>.json`; existing files are resolved with `DefaultKeyComparer` and retain their original names. `AvailableParts` lists matching files without extensions and rejects conflicting names. JSON file extensions are matched without case sensitivity.

Operations use per-key locks within that instance. Saving removes the previous file and writes the replacement directly; there is no backup or atomic replacement protocol. A failed write can lose the previous value. Separate instances or processes do not share these locks. Reads load the current file, so external edits can be seen by later reads.

When a missing key is created by `Get`, this backend writes it directly instead of calling public `Set`; it does not emit `OnChanged` for that default creation.

## JsonOneFileConfiguration: one document

```csharp
using Asv.Cfg;

using var configuration = new JsonOneFileConfiguration(
    fileName: "settings/application.json",
    createIfNotExist: true,
    flushToFileDelayMs: null,
    sortKeysInFile: true);
configuration.Set("RetryCount", 3);
Console.WriteLine(configuration.FileName);
```

The first three constructor arguments are required. Optional arguments are `sortKeysInFile`, `logger`, `fileSystem`, and `timeProvider`. Despite its name, `flushToFileDelayMs` accepts `TimeSpan?`, not a number of milliseconds. Pass null for immediate saves.

The file contains one JSON object whose properties are configuration keys. The store loads it into a case-insensitive token dictionary at construction. It does not watch external changes. Sorting orders the top-level keys when writing; it does not recursively sort every object.

## Save and recovery sequence

1. Write the complete current dictionary to `<file>.backup` using write-through file options.
2. Flush and close the backup stream.
3. Copy the backup over the main file.

The backup remains after a successful save and contains the latest saved payload, not necessarily a previous version. This reduces some corruption risks but is not an atomic rename or a multi-process locking scheme.

At startup, invalid JSON in the main file is deleted. If the main file is missing, a valid backup can be restored when `createIfNotExist` is true. A corrupt backup is removed during that recovery path. With creation enabled and no usable file, the store creates `{}`; with creation disabled, missing data raises an exception. JSON syntax validation does not guarantee the expected object schema or compatible settings types.

## Deferred saves

A non-null delay uses R3 `ThrottleLast` with the supplied `TimeProvider`, defaulting to `TimeProvider.System`. Values change in memory first; writes occur on the throttled schedule. `OnChanged` therefore does not prove that a delayed write has reached disk.

The current public API has no explicit `Flush` method. Disposal tears down the save subscriptions but contains no explicit final-save step, so do not rely on it as a durable flush guarantee for pending delayed changes. Use immediate saves when persistence must finish before returning or shutting down.

Save failures are published to `OnError`. With deferred saves they occur after the original call and cannot be handled solely by catching exceptions around `Set`. See [Integration](Cfg-Integration.md).

## Testing file behavior

Inject `IFileSystem` to isolate file operations and `TimeProvider` for deterministic deferred-save checks. Existing tests use `MockFileSystem` from `TestableIO.System.IO.Abstractions.TestingHelpers`. These optional constructor arguments are available on the folder and one-file implementations; not every backend accepts them.
