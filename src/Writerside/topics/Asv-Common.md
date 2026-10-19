# Asv.Common library

`Asv.Common` contains the shared primitives used by the other packages in this repository. Its public types live in the `Asv.Common` namespace. The library targets the framework configured in `src/Directory.Build.props`, currently `net10.0`.

Unlike the repository-level [overview](overview.md), this section describes the `Asv.Common` NuGet package itself.

## Installation

```bash
dotnet add package Asv.Common
```

The package depends on R3 for reactive helpers and Geodesy for geographic calculations. Configuration providers, protocol transports, application models, and storage formats belong to the neighboring packages described in [Modules](modules.md).

## Choose a guide

| Task | Main types | Guide |
| --- | --- | --- |
| Own resources and subscriptions | `DisposableOnce`, `AsyncDisposableOnce`, `DisposableHelper` | [Resource lifetime and execution](Common-Lifetime.md) |
| Keep a bounded history or manipulate bit fields | `CircularBuffer2<T>`, `UintBitArray` | [Collections and bit fields](Common-Collections.md) |
| Parse user input and handle validation failures | `InvariantNumberParser`, `ValidationResult`, `LocalizedException` | [Values, validation, and utilities](Common-Values.md) |
| Work with time, identifiers, or display names | `ITimeService`, `ShortGuid`, `WordFingerprint` | [Values, validation, and utilities](Common-Values.md) |

## First example

`InvariantNumberParser` returns a validation result instead of requiring callers to catch an exception for ordinary invalid input:

```csharp
using Asv.Common;

var result = InvariantNumberParser.TryParse("2.5k", out double value);
if (result.IsSuccess)
{
    Console.WriteLine(value); // 2500
}
else
{
    Console.WriteLine(result.ValidationException?.Message);
}
```

## Source and verification

Implementation is under `src/Asv.Common`; the matching tests are under `src/Asv.Common.Test`. Run these commands from the repository root:

```bash
dotnet build src/Asv.Common/Asv.Common.csproj
dotnet test src/Asv.Common.Test/Asv.Common.Test.csproj
```

For repository setup and the other packages, see [Getting Started](getting-started.md).
