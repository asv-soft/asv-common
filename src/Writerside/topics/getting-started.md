# Getting Started

## Prerequisites

- .NET SDK compatible with the target frameworks configured in `src/Directory.Build.props`.
- JetBrains Rider, Visual Studio, or another IDE that can open `src/Asv.Common.slnx`.
- Writerside plugin or standalone Writerside if you want to edit this documentation with preview and inspections.

## Restore, Build, And Test

Run commands from the repository root:

```bash
dotnet restore src/Asv.Common.slnx
dotnet build src/Asv.Common.slnx
dotnet test src/Asv.Common.slnx
```

For work limited to one package, build the package project and its matching test project directly:

```bash
dotnet build src/Asv.Common/Asv.Common.csproj
dotnet test src/Asv.Common.Test/Asv.Common.Test.csproj
```

## Repository Layout

```text
src/
  Asv.Common/          common primitives
  Asv.Cfg/             configuration providers
  Asv.Composition/     composition helpers
  Asv.IO/              protocols, transport, and serialization
  Asv.Modeling/        model tree, navigation, layout, and undo primitives
  Asv.Store/           package and hierarchical storage
  Asv.XUnit/           xUnit test helpers
  Writerside/          repository documentation
```

## Adding Documentation

Add new documentation as Markdown files under `src/Writerside/topics` and register them in `src/Writerside/asv-common.tree`.

Use concise, descriptive file names. The topic file name becomes the generated page URL, so prefer names such as `getting-started.md` or `configuration.md` over generic names such as `topic.md`.
