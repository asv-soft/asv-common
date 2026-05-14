![linkedin](https://github.com/user-attachments/assets/4fa5221e-7ae5-4b6b-98a8-1c1e39b49afb)

# asv-common

A set of shared .NET libraries for ASV projects: core utilities, configuration,
I/O, application modeling, data storage, and supporting infrastructure for tests.

## Libraries

| Library | Purpose | Main Functionality |
| --- | --- | --- |
| `Asv.Common` | Common primitives and extensions. | `IDisposable`/`IAsyncDisposable` lifecycle helpers, async locks, keyed locks, concurrent collections, localized validation errors, math and geodesy helpers, angle types, CRC, time services, link indicators, CSV/text/table helpers, semantic versioning, and short GUIDs. |
| `Asv.Cfg` | Unified configuration layer. | `IConfiguration`/`IConfigurationReader`, in-memory configuration, single-file JSON configuration, folder storage, package/ZIP storage, and versioned configuration files. |
| `Asv.Composition` | Lightweight module composition. | Module contracts based on `System.Composition`, named exports, dependency metadata, and helpers for composition containers. |
| `Asv.IO` | Protocol and I/O toolkit. | Serial/TCP/UDP ports and endpoints, protocol connections, parsers, routers, message formatters, device and microservice abstractions, device explorer, binary/bit serializers, streams, visitable schemas, and compression helpers. |
| `Asv.Modeling` | Application model primitives. | Identifiers, hierarchy, ordering, focus, change tracking, routed events, navigation controller/store, undo/redo controller, and undo history. |
| `Asv.Store` | Persistent data storage. | ASV package parts for metadata, arrays, key-value data and time series, hierarchical file-system store, list data files, JSON/MessagePack/XML formats, and CHIMP time-series encoding. |
| `Asv.XUnit` | Helper tools for xUnit v3. | Attributes for local and manual tests, logger adapters for `ITestOutputHelper`. |
| `Asv.Common.Shell` | Local utility project. | Console commands and benchmarks for development. |

## Build

The solution targets .NET 10. Shared dependency versions are defined in
`src/Directory.Build.props`.

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
  Asv.Modeling/     navigation, events, and undo for models
  Asv.Store/        package and hierarchical storage
  Asv.XUnit/        test helpers
```
