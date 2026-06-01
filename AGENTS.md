# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## Overview

**asv-common** is a multi-package .NET library suite (targeting `net10.0`) published to NuGet. It provides reusable components for reactive, modular applications. All source lives under `src/`; there is no solution file — projects are linked via `src/Directory.Build.props`.

## Build Commands

```powershell
# Restore
dotnet restore src

# Build (Release)
dotnet build src -c Release

# Pack NuGet packages (after build)
dotnet pack src -c Release --no-build --no-restore
```

## Testing

```powershell
# Run all tests
dotnet test src

# Run tests for one module
dotnet test src/Asv.Modeling.Test

# Run a specific test by name filter
dotnet test src/Asv.Modeling.Test --filter "DisplayName~MyTestName"
```

Tests marked with `[LocalFact]` / `[LocalTheory]` (from **Asv.XUnit**) are skipped in CI — use standard `[Fact]`/`[Theory]` for code that must run everywhere.

## Version Management

All packages share a single version in `src/Directory.Build.props`:

```xml
<ProductVersion>3.6.0-dev.9</ProductVersion>
```

Every `.csproj` inherits `Version`, `PackageVersion`, and `FileVersion` from this property — do not set them per-project.

## Code Quality

- **Nullable reference types** are enabled across all library projects (`<Nullable>enable</Nullable>`).
- **`<LangVersion>latest`** — use current C# features freely.
- Specific warning codes are promoted to errors (see `WarningsAsErrors` in any `.csproj`). The most important are nullable violations (`CS8600`–`CS8604`, `CS8625`, `CS8629`, `CS8762`) and obsolete API usage (`CS0618`).
- StyleCop + Roslynator analyzers are active. Rules are configured in `src/CodeStyle.ruleset` — check there before suppressing any analyzer warning.

## Architecture

### Packages and Dependencies

```
Asv.Common          ← base types, R3 reactive helpers, math, CRC, time, units
Asv.Cfg             ← IConfiguration abstraction + JSON/ZIP backends
Asv.IO              ← IDataStream, protocol factories, device microservices
Asv.Composition     ← MEF-based module discovery and dependency sorting
Asv.Modeling        ← behavioral composition (undo, nav, hierarchy, focus, events)
Asv.Store           ← hierarchical persistent storage, ASV package format
Asv.XUnit           ← xUnit helpers (LocalFact, LoggerWrapper)
```

`Asv.Common` has no internal dependencies. `Asv.Cfg`, `Asv.IO`, and `Asv.Modeling` depend on `Asv.Common`. `Asv.Store` depends on `Asv.IO`.

### Reactive Programming (R3)

All observable streams use **R3** (not System.Reactive). Mutable state is exposed through `ReactiveProperty<T>` / `ReadOnlyReactiveProperty<T>`. Subscribe via R3 operators; do not use classic C# events for inter-component communication.

### Modeling Behaviors (Asv.Modeling)

Objects opt into behaviors by implementing support interfaces rather than inheriting a fat base class:

| Interface | Behavior |
|-----------|----------|
| `ISupportChanges` | Tracks unsaved changes; `HasChanges` property |
| `ISupportUndo` / `IHasUndoHistory` | Undo/redo stack |
| `ISupportChildren` / `ISupportParent` | Tree hierarchy |
| `ISupportNavigation` / `INavigationRoot` | NavId/NavPath addressing |
| `ISupportFocus` | Focus state |
| `ISupportRoutedEvents` | Event bubbling/tunneling through the tree |
| `ISupportId` | ULID-based unique identity |

Compose only the interfaces an object needs; do not inherit from a monolithic base.

### Configuration Backends (Asv.Cfg)

All backends implement `IConfiguration`. Swap implementations without changing consumers:

- `InMemoryConfiguration` — tests and ephemeral state
- `JsonConfiguration` — one JSON file per key in a folder
- `JsonOneFileConfiguration` — single JSON file
- `JsonPackageConfiguration` / `ZipJsonConfiguration` — ZIP-backed stores

### Composition / Modules (Asv.Composition)

Modules are MEF exports that implement `IModule`. Declare inter-module dependencies with `[ExportDependency("OtherModuleName")]`. `CompositionHostHelper` resolves load order and detects circular or missing dependencies at startup.

### I/O / Device Layer (Asv.IO)

- `IDataStream` — lowest-level send/receive for any byte channel (serial, TCP, etc.)
- `IProtocolFactory` — constructs typed protocol instances from stream pairs
- `IMicroservice` / `IMicroserviceContext` — device-level abstraction; context acts as a scoped DI container

### Serialization

- Binary/zero-allocation: implement `ISizedSpanSerializable` (span-based read/write with fixed size)
- Structured data: MessagePack or MemoryPack

### Naming Conventions

- Interfaces: `IFoo`
- Behavioral/support interfaces: `ISupportFoo`
- Extension/helper statics: `FooHelper` or `FooMixin`
- Internal resource strings: `RS.resx` / `RS.Designer.cs` per project

## Comments and Documentation

- Write all code comments, XML doc, Markdown, and README content in **English only** — no Russian or mixed-language text.
- Use clear English names for types, members, variables, files, modules, and public APIs.
- Keep terminology consistent across code, comments, and documentation.
- Add comments only when they explain intent, constraints, assumptions, tradeoffs, or non-obvious behavior. Do not restate what the code already makes obvious.
- Keep comments concise and up to date — remove or update them when the code changes.

## Design Principles

- Follow SOLID principles. Each class/service has a single, well-defined responsibility.
- Prefer composition over inheritance unless inheritance is clearly justified.
- Minimize coupling; separate domain logic from UI, infrastructure, persistence, and framework concerns.
- Depend on abstractions at system boundaries when this improves testability or extensibility.
- Keep public APIs explicit, stable, and easy to understand.
- Eliminate duplicated logic through extraction rather than copying.
- Avoid god objects, hidden side effects, and unclear ownership of responsibilities.

## Coding Guidelines

**Think before coding:** State assumptions explicitly. If multiple interpretations exist, present them rather than picking silently. If something is unclear, ask before implementing. If a simpler approach exists, say so; push back when warranted.

**Simplicity first:** Write the minimum code that solves the problem. No speculative features, no abstractions for single-use code, no "flexibility" that wasn't requested, no error handling for impossible scenarios.

**Surgical changes:** Touch only what the task requires. Do not improve adjacent code, comments, or formatting. Match existing style. If you notice unrelated dead code, mention it — don't delete it. Remove only imports/variables/functions that *your* changes made unused.

**Goal-driven execution:** For multi-step tasks, state a brief plan with verifiable steps before starting. Define success criteria and loop until the change is verified, for example by adding or running focused tests for bug fixes and validation changes.
