# Asv.Cfg

`Asv.Cfg` stores typed configuration objects under string keys. It provides a common API for in-memory values, a folder of JSON files, a single JSON file, a package part, and ZIP entries. All public types use the `Asv.Cfg` namespace.

## Installation

```bash
dotnet add package Asv.Cfg
```

The project currently targets `net10.0` through `src/Directory.Build.props`. It uses Newtonsoft.Json, R3 notifications, Asv.Common lifetime helpers, and file-system abstractions.

## First configuration

```csharp
using Asv.Cfg;

using var configuration = new InMemoryConfiguration();
var settings = configuration.Get<ConnectionSettings>();
settings.Host = "localhost";
configuration.Set(settings);

public sealed class ConnectionSettings
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 7341;
}
```

The type-based overload uses `ConnectionSettings` as its key. Loading an absent key stores the default object. Changing the returned object alone does not save it: call `Set` or `Update` explicitly.

## Choose a backend

| Backend | Representation | Appropriate use |
| --- | --- | --- |
| `InMemoryConfiguration` | JSON tokens held in memory | Tests and temporary settings |
| `JsonConfiguration` | One `<key>.json` file per value | Independently editable configuration sections |
| `JsonOneFileConfiguration` | One JSON object containing all keys | A single application settings file |
| `JsonPackageConfiguration` | One JSON object in a `Package` part | Settings embedded in an existing package |
| `ZipJsonConfiguration` | One `<key>.json` entry per value | Portable ZIP settings bundles |
| `ZipJsonVersionedFile` | ZIP settings plus `FileInfo.json` | Bundles with a file type and maximum supported version |

These backends share operations, but differ in notifications on default creation, persistence timing, and ownership. Choose based on the details below rather than assuming they are interchangeable in every respect.

## Guides

- [Keys, typed values, and updates](Cfg-Values.md)
- [In-memory and JSON file backends](Cfg-Json.md)
- [Packages and ZIP archives](Cfg-Archives.md)
- [Versioned files](Cfg-Versioning.md)
- [Notifications, lifetime, and extension points](Cfg-Integration.md)

Configuration serialization is not encryption. Keep credentials in an appropriate secret store and avoid writing configuration values to logs.

## Source and verification

Sources are under `src/Asv.Cfg`; tests are under `src/Asv.Cfg.Test`.

```bash
dotnet build src/Asv.Cfg/Asv.Cfg.csproj
dotnet test src/Asv.Cfg.Test/Asv.Cfg.Test.csproj
```
