# Asv.Modeling

`Asv.Modeling` provides UI-independent building blocks for application models: identity, ordering, trees, focus, change tracking, routed events, navigation, layout persistence, and undo/redo. Types live in the `Asv.Modeling` namespace. The package targets the framework configured in `src/Directory.Build.props`, currently `net10.0`.

## Installation

```bash
dotnet add package Asv.Modeling
```

Models opt into small support interfaces and compose controllers instead of inheriting one mandatory model base class. The package depends on Asv.Common; it does not require Avalonia controls or view models.

## Choose a guide

| Task | Contracts and components | Guide |
| --- | --- | --- |
| Expose parent and child relationships | `ISupportParent<T>`, `ISupportChildren<T>`, `ISupportParentChange<T>` | [Tree](Tree.md) |
| Dispatch an operation through the hierarchy | `ISupportRoutedEvents<T>`, `RoutedEventController` | [Routed events](RoutedEvents.md) |
| Resolve a model by a stable path and track navigation history | Navigation controllers and paths | [Navigation](Navigation.md) |
| Save and restore model state | Layout registrations, controllers, and stores | [Layout](Layout.md) |
| Record and replay model changes | Undo controllers, handlers, histories, and stores | [Undo](Undo.md) |

## Small support contracts

| Contract | Meaning | Application responsibility |
| --- | --- | --- |
| `ISupportId<TId>` | Exposes a read-only `Id`. | Choose an identity stable enough for the model's lifetime and persistence needs. |
| `ISupportOrder` | Exposes integer `Order`; its `Comparer.Instance` compares ascending values, placing null before non-null. | Assign order values and decide how equal values are ordered. |
| `ISupportFocus` | Exposes `Focus()`. | Implement what focus means; the interface itself does not store focus state or select a UI control. |
| `ISupportChangeTracking` | Exposes `ReadOnlyReactiveProperty<bool> HasChanges`. | Maintain the state and define when saving resets it. |

These interfaces do not automatically add navigation, persistence, or undo. Implement and register each required behavior explicitly.

```csharp
using Asv.Modeling;

public sealed class DocumentIdentity(string id, int order)
    : ISupportId<string>, ISupportOrder
{
    public string Id { get; } = id;
    public int Order { get; } = order;
}
```

## Integration order

1. Define stable identities and parent/child ownership.
2. Add routed events when operations must travel through the tree.
3. Add navigation for resolving nodes and addressing persisted operations.
4. Register the state required by layout persistence.
5. Register undo handlers and attach the intended history owner.

A change to a property does not become undoable just because the object supports change tracking. Layout snapshots and undo history also have different lifecycles: one restores application state, while the other records reversible operations.

## Lifetime and persistence

The application owns controllers, subscriptions, and persistent stores. Dispose them with their owner and avoid retaining detached nodes through subscriptions. Root-dependent features should follow the attachment rules in [Tree](Tree.md).

Keep persisted identifiers, navigation paths, layout keys, and undo change identifiers stable. Renaming them can make saved state or history impossible to resolve even when the C# code still compiles. Thread scheduling and UI-thread transitions belong to the integrating application.

## Source and verification

Implementation is under `src/Asv.Modeling`; examples and tests are under `src/Asv.Modeling.Test`.

```bash
dotnet build src/Asv.Modeling/Asv.Modeling.csproj
dotnet test src/Asv.Modeling.Test/Asv.Modeling.Test.csproj
```
