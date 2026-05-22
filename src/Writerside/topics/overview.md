# Asv.Common

%product% is a set of shared .NET libraries used by ASV projects. It provides reusable primitives for configuration, I/O, composition, application modeling, persistent storage, and test infrastructure.

Use this documentation as the entry point for repository-level guidance. API-level details remain in XML documentation comments and tests next to the implementation.

## What Is Included

The repository contains several focused packages:

| Package | Purpose |
| --- | --- |
| `Asv.Common` | Common primitives, async helpers, collections, validation errors, math helpers, geodesy utilities, CRC, time services, and versioning helpers. |
| `Asv.Cfg` | Configuration abstractions and providers for in-memory, file, folder, package, and versioned configuration storage. |
| `Asv.Composition` | Lightweight module composition helpers based on `System.Composition`. |
| `Asv.IO` | Protocol, transport, serialization, routing, stream, schema, compression, device, and microservice utilities. |
| `Asv.Modeling` | Application model primitives for identifiers, hierarchy, focus, routed events, navigation, layout, and undo/redo flows. |
| `Asv.Store` | Persistent package and hierarchical storage for metadata, arrays, key-value data, and time-series data. |
| `Asv.XUnit` | xUnit v3 helpers for local/manual tests and test logging. |

## Documentation Scope

This Writerside help is intentionally small. Keep it focused on:

- Repository structure and module boundaries.
- Build, test, and development workflow.
- Cross-package conventions that are not obvious from code.
- Links between concepts that span multiple projects.

Avoid duplicating public API reference text here unless it explains how several APIs work together.
