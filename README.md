![linkedin](https://github.com/user-attachments/assets/4fa5221e-7ae5-4b6b-98a8-1c1e39b49afb)

# asv-common

[![NuGet version](https://img.shields.io/nuget/v/Asv.Common)](https://www.nuget.org/packages/Asv.Common)
[![Build and tests](https://github.com/asv-soft/asv-common/actions/workflows/code-quality.yml/badge.svg?branch=main)](https://github.com/asv-soft/asv-common/actions/workflows/code-quality.yml)
[![Documentation](https://github.com/asv-soft/asv-common/actions/workflows/docs.yml/badge.svg?branch=main)](https://asv.me/asv-common/)
[![GitHub Packages](https://img.shields.io/badge/GitHub-Packages-blue?logo=github)](https://github.com/asv-soft/asv-common/pkgs/nuget/Asv.Common)

A set of shared .NET libraries for ASV projects: core utilities, configuration,
I/O, application modeling, data storage, and supporting infrastructure for tests.

## Documentation

Read the [Asv.Common documentation](https://asv.me/asv-common/) for package
overviews, configuration storage, I/O, application modeling, and development guides.

## Libraries

| Library            | Purpose                           | Main Functionality                                                                                                                                                                                                                                                           |
|--------------------|-----------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Asv.Common`       | Common primitives and extensions. | `IDisposable`/`IAsyncDisposable` lifecycle helpers, async locks, keyed locks, concurrent collections, localized validation errors, math and geodesy helpers, angle types, CRC, time services, link indicators, CSV/text/table helpers, semantic versioning, and short GUIDs. |
| `Asv.Cfg`          | Unified configuration layer.      | `IConfiguration`/`IConfigurationReader`, in-memory configuration, single-file JSON configuration, folder storage, package/ZIP storage, and versioned configuration files.                                                                                                    |
| `Asv.Composition`  | Lightweight module composition.   | Module contracts based on `System.Composition`, named exports, dependency metadata, and helpers for composition containers.                                                                                                                                                  |
| `Asv.IO`           | Protocol and I/O toolkit.         | Serial/TCP/UDP ports and endpoints, protocol connections, parsers, routers, message formatters, device and microservice abstractions, device explorer, binary/bit serializers, streams, visitable schemas, and compression helpers.                                          |
| `Asv.Modeling`     | Application model primitives.     | Identifiers, hierarchy, ordering, focus, change tracking, routed events, navigation, layout management and persistence, undo/redo, and undo history.                                                                                                                         |
| `Asv.Store`        | Persistent data storage.          | ASV package parts for metadata, arrays, dictionaries, key-value data, files, and time series; hierarchical file-system storage; list data files; JSON/MessagePack/XML formats; and CHIMP time-series encoding.                                                               |
| `Asv.XUnit`        | Helper tools for xUnit v3.        | Attributes for local and manual tests, logger adapters for `ITestOutputHelper`.                                                                                                                                                                                              |
| `Asv.Common.Shell` | Local utility project.            | Console commands and benchmarks for development.                                                                                                                                                                                                                             |

## Build

The solution targets .NET 10. Shared project settings, including the target
framework and package version, are defined in `src/Directory.Build.props`.
NuGet dependency versions are declared in the individual project files.

```bash
dotnet restore src/Asv.Common.slnx
dotnet build src/Asv.Common.slnx
dotnet test src/Asv.Common.slnx
```

## Project Structure

```text
src/
  Asv.Common/       common primitives
  Asv.Cfg/          configuration providers
  Asv.Composition/  composition helpers
  Asv.IO/           protocols, transport, and serialization
  Asv.Modeling/     navigation, layout, events, and undo for models
  Asv.Store/        package and hierarchical storage
  Asv.XUnit/        test helpers
  Asv.Common.Shell/ development commands and benchmarks
```

## Links

- Documentation: [asv.me/asv-common](https://asv.me/asv-common/)
- Repository: [asv-soft/asv-common](https://github.com/asv-soft/asv-common)
- Issues: [GitHub Issues](https://github.com/asv-soft/asv-common/issues)
