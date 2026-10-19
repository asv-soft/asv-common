# Keys, values, and updates

## Contracts

`IConfigurationReader` exposes `ReservedParts`, `AvailableParts`, `Exist(key)`, and `Get<T>(key, Lazy<T>)`. Its read-oriented name does not mean reads are side-effect free: an absent value is normally created and stored by `Get`.

`IConfiguration` adds `Set<T>(key, value)`, `Remove(key)`, `OnChanged`, `OnError`, and `IDisposable`. Concrete stores inherit `ConfigurationBase`, which also supports asynchronous disposal through Asv.Common.

## Key rules

`ConfigurationMixin.ValidateKey` accepts keys matching `^(?!\d)[\w$]+$`. Keys cannot be empty, whitespace, or start with a digit; dots, slashes, spaces, and hyphens are rejected. The regular expression uses .NET Unicode word characters, not only ASCII letters.

Use simple stable keys such as `ConnectionSettings`, `ui_theme`, or `$metadata`. Invalid keys passed through a `ConfigurationBase` operation are reported as `ConfigurationException`. Calling `ValidateKey` directly throws an argument exception.

All backends use `ConfigurationMixin.DefaultKeyComparer` (`StringComparer.InvariantCultureIgnoreCase`) for configuration key identity. `Host`, `host`, and `HOST` refer to the same setting for `Exist`, `Get`, `Set`, and `Remove`, independently of the file system. This comparer can also equate some Unicode spellings; do not model key identity with ASCII lowercasing.

Updating an existing setting preserves its stored spelling: a folder file `Host.json`, ZIP entry `Host.json`, or JSON property `Host` retains that name when updated through `HOST`. New keys use the spelling supplied by the caller. No bulk renaming or automatic migration is performed.

### Conflicting existing keys

If a folder or ZIP archive contains multiple names equal under the comparer, accessing that key or enumerating `AvailableParts` throws `ConfigurationException` before choosing, deleting, or overwriting a value. Even an exact spelling does not bypass the conflict. Unrelated non-conflicting keys can still be accessed.

Single-document JSON and package JSON reject conflicting top-level properties while loading, including exact duplicate property names. They do not silently keep the first or last value. Resolve conflicts explicitly after backing up the original: select or merge values, then retain one spelling.

### Compatibility

Public method signatures and storage formats are unchanged, but key identity is an intentional behavior change. In-memory keys and file names on case-sensitive file systems that previously represented separate values now collide. ZIP updates and removals now find the same entry as reads regardless of spelling. Applications relying on distinct case-only keys must migrate their names before adopting this behavior.

`AvailableParts` enumerates stored keys. `ReservedParts` describes backend metadata keys; it does not prevent overwriting or removing them.

## Reading and writing

```csharp
using Asv.Cfg;

using var configuration = new InMemoryConfiguration();
var value = configuration.Get("RetryCount", new Lazy<int>(() => 3));
configuration.Set("RetryCount", value + 1);
var saved = configuration.Get("RetryCount", 3);
configuration.Remove("RetryCount");
```

The default factory is evaluated when needed. A missing ordinary key is persisted before `Get` returns; therefore `Get` can fail because a write failed. For `ICustomConfigurable`, the factory is evaluated to obtain the object whose `Load` method runs, even if related data already exists.

JSON-backed values are serialized snapshots. Treat the loaded object as a detached value and save it after editing. Malformed or incompatible existing data is not generally replaced with defaults: deserialization can throw. The in-memory implementation falls back to the supplied default if converting an existing token returns null; other backends can reject that case.

## Type-based shortcuts

`Get<T>()`, `Set<T>(value)`, and `Remove<T>()` use `typeof(T).Name`. `Get<T>(key)` creates defaults with `new T()`. The type-based operations and `Update<T>` require a public parameterless constructor.

Renaming a settings type changes its implicit key. Types with the same simple name in different namespaces share an implicit key. Generic type names can contain characters rejected by key validation. Use explicit stable keys when these constraints matter.

```csharp
using Asv.Cfg;

using var configuration = new InMemoryConfiguration();
configuration.Update<RetrySettings>(settings => settings.Attempts = 5);

public sealed class RetrySettings
{
    public int Attempts { get; set; } = 3;
}
```

`Update<T>` is a read/callback/write convenience method, not a transaction or atomic compare-and-swap. Concurrent updates can overwrite each other. For value types the callback receives a copy, so prefer mutable reference-type settings for this helper. Coordinate related or concurrent updates in the application.

## JSON conventions

`JsonHelper.CreateDefaultJsonSerializer()` uses indented Newtonsoft.Json output and a string-enum converter with camel-case naming and integer values allowed. Most persistent backends use this helper. The in-memory backend uses `JsonConvert` defaults instead, and `JsonConfigurationBase` converts loaded tokens with `JToken.ToObject<T>()` without passing its write serializer. Do not assume identical custom converter behavior across backends.

For durable configuration, preserve property names and value meanings, supply sensible initializers for new properties, and explicitly migrate incompatible schema changes.
