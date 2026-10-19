# Notifications, lifetime, and extension points

## Change notifications

`OnChanged` is an R3 `Observable<KeyValuePair<string, object?>>`. A successful ordinary `Set` emits the key and the supplied value; `Remove` emits the key and null, even if the key did not exist. The event is not a deep copy of the object supplied to `Set`.

```csharp
using Asv.Cfg;
using R3;

using var configuration = new InMemoryConfiguration();
using var subscription = configuration.OnChanged.Subscribe(change =>
    Console.WriteLine($"Changed: {change.Key}"));
configuration.Set("RetryCount", 3);
configuration.Remove("RetryCount");
```

Default creation by `Get` emits a change in the in-memory and `JsonConfigurationBase` implementations because they call public `Set`. Folder JSON and plain ZIP create defaults internally and do not emit that event. Custom loading and saving publish only the notifications caused by the operations their implementation invokes.

Notifications run through the current call path; deferred JSON writes use the configured time provider's scheduling. No UI-thread dispatch is provided. Marshal to the intended thread in the integrating application. Avoid recursive writes to the same key from a change observer.

## Errors

`ConfigurationBase` validates keys and wraps failures from its public operations in `ConfigurationException`, publishing the error on `OnError` before throwing. Constructor failures occur before a caller can subscribe and may be argument, I/O, JSON, or configuration exceptions.

```csharp
using Asv.Cfg;
using R3;

using var configuration = new InMemoryConfiguration();
using var errors = configuration.OnError.Subscribe(error =>
    Console.Error.WriteLine(error.Message));
try
{
    configuration.Set("invalid-key", 3);
}
catch (ConfigurationException error)
{
    Console.Error.WriteLine(error.Message);
}
```

Subscribing to `OnError` does not consume a thrown exception. Immediate writes normally report failures during the call. Deferred `JsonConfigurationBase` writes publish failures later and suppress rethrow from that save callback; observe `OnError` for them. Deferred events do not provide a per-value durable-save acknowledgement.

`AvailableParts` can expose a lazy backend enumeration; failures during later enumeration need not pass through the property's initial exception wrapper. Keep file enumeration handling at the call site too.

## Custom object persistence

Implement `ICustomConfigurable` when one logical object needs custom keys or a different representation. `ConfigurationBase.Get<T>` creates or obtains the default instance and calls `Load(key, configuration)` instead of the backend's normal object deserializer. `Set` calls `Save` instead of saving that object as a token.

```csharp
using Asv.Cfg;

public sealed class CustomSettings : ICustomConfigurable
{
    public int Attempts { get; set; } = 3;

    public void Load(string key, IConfiguration configuration)
    {
        Attempts = configuration.Get(key + "_attempts", 3);
    }

    public void Save(string key, IConfiguration configuration)
    {
        configuration.Set(key + "_attempts", Attempts);
    }
}
```

The derived key still follows normal validation. Do not call `Get<CustomSettings>(key)` or `Set(key, this)` recursively inside these hooks. `Exist(key)` and `Remove(key)` operate on the literal key; they do not discover or delete the custom object's derived keys. Define application-level cleanup when a custom object occupies several keys.

## Implement a backend

Derive from `ConfigurationBase` to implement reserved/available key enumeration, existence checks, reading, saving, and removal through its protected `InternalSafe*` methods. Let the base class own public validation and notification behavior. Choose locking and snapshot semantics explicitly; the base class does not make all operations transactional.

For a whole-document JSON backend, derive from `JsonConfigurationBase`. Supply a load-stream callback and implement `BeginSaveChanges`, `EndSaveChanges`, and reserved-key enumeration. The base owns and closes the returned read/write streams; the backend owns the enclosing resource. See [JSON backends](Cfg-Json.md) for deferred-write limitations.

## Resource ownership

Use `using` for `IConfiguration` and dispose subscriptions before their owner. Concrete stores also inherit asynchronous disposal from `ConfigurationBase`. ZIP owns its archive, folder JSON operates on short-lived file streams, one-file JSON owns its save subscription, and package JSON leaves the caller's package open.

There is no shared transaction, automatic file watcher, merge of concurrent writers, or general multi-process synchronization contract. Application code must supply these behaviors if its workflow requires them.

## Implementation map

| Area | Source |
| --- | --- |
| Public contracts and typed helpers | `IConfigurationReader.cs`, `IConfiguration.cs`, `ConfigurationMixin.cs` |
| Validation, notifications, custom hooks, and errors | `ConfigurationBase.cs`, `ICustomConfigurable.cs`, `ConfigurationException.cs` |
| Memory backend | `InMemory/InMemoryConfiguration.cs` |
| Shared JSON serialization and whole-document writes | `Json/JsonHelper.cs`, `Json/JsonConfigurationBase.cs` |
| Folder, file, and package | `Json/Folder`, `Json/File`, `Json/Package` |
| ZIP and version metadata | `Json/Zip` |

The existing test suite covers the common contract and the in-memory, folder, one-file, ZIP, and versioned ZIP implementations. `ConfigurationKeyComparerTest` also exercises package creation, mixed-case updates, duplicate rejection, and persistence across configuration instances.
