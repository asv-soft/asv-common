# Modules

## Dependency Direction

Keep dependencies flowing from specialized packages toward shared primitives:

```text
Asv.Common
  <- Asv.Cfg
  <- Asv.Composition
  <- Asv.IO
  <- Asv.Modeling
Asv.IO <- Asv.Store
Asv.Modeling <- Asv.Store
Asv.XUnit
```

When adding a new dependency, check that it does not create a cycle or pull implementation-specific concerns into a lower-level package.

## Package Boundaries

| Package | Add code here when it is about |
| --- | --- |
| `Asv.Common` | Cross-cutting primitives that are useful without domain-specific dependencies. |
| `Asv.Cfg` | Reading, writing, storing, and versioning configuration data. |
| `Asv.Composition` | Module discovery, named exports, metadata, and composition container helpers. |
| `Asv.IO` | Transport, protocol parsing, binary serialization, endpoint routing, and device communication. |
| `Asv.Modeling` | UI-agnostic application model behavior such as identity, tree structure, focus, navigation, layout, and undo. |
| `Asv.Store` | Persistent storage formats, packages, hierarchical stores, and time-series data. |
| `Asv.XUnit` | Shared test-only helpers for xUnit-based projects. |

## Test Placement

Put tests in the matching `*.Test` project:

- `Asv.Common` -> `Asv.Common.Test`
- `Asv.Cfg` -> `Asv.Cfg.Test`
- `Asv.IO` -> `Asv.IO.Test`
- `Asv.Modeling` -> `Asv.Modeling.Test`
- `Asv.Store` -> `Asv.Store.Test`

Prefer focused tests near the behavior being changed. Broaden coverage when the change affects a shared contract, serialization format, or cross-package workflow.
