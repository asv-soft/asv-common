# Devices And Identification

The device layer turns a protocol message stream into logical devices and microservices. Identity is provided by registered factories; neither endpoint addresses nor concrete protocol headers are assumed by the shared explorer.

## Core Contracts

```mermaid
flowchart TD
    Connection["IProtocolConnection.OnRxMessage"] --> Explorer["IDeviceExplorer"]
    Explorer --> Factories["IClientDeviceFactory, ordered by Order"]
    Factories --> Identity["DeviceId"]
    Factories --> Device["IClientDevice"]
    Device --> Services["IMicroserviceClient"]
    Context["IMicroserviceContext"] -. connection, time, logging, metrics .-> Device
```

| Contract | Responsibility |
| --- | --- |
| `DeviceId` | Represents a protocol-specific logical device identity and its string form. |
| `IClientDeviceFactory` | Identifies messages, creates devices, and updates existing devices. |
| `IDeviceExplorer` | Observes discovery traffic, exposes devices, and offers identity-only lookup. |
| `IClientDevice` | Exposes identity, name, initialization state, link state, and microservices. |
| `IClientDeviceExtender` | Contributes device-specific extensions through the configured lifecycle. |
| `IMicroserviceContext` | Supplies connection, logger factory, time provider, and metrics. |

## Register Factories

```csharp
using Asv.IO;
using Asv.IO.Device;

var factory = Protocol.Create(builder =>
{
    builder.Protocols.RegisterExampleProtocol();
});
await using var link = factory.CreateVirtualConnection(printMessagesToLog: false);
await using var explorer = DeviceExplorer.Create(link.Client, builder =>
{
    builder.Factories.RegisterExampleDevice(new ExampleDeviceConfig());
});

var message = new ExampleMessage1 { SenderId = 7 };
if (explorer.TryIdentify(message, out var deviceId))
{
    Console.WriteLine(deviceId.AsString());
}
Console.WriteLine(explorer.Devices.Count); // 0: identification did not discover a device.
```

The explorer snapshots the registered factories and orders them by ascending `Order`. It selects the first factory that returns `true` with a non-null ID. A rejected factory does not prevent later factories from matching.

## Identification Without Discovery

`IDeviceExplorer.TryIdentify(IProtocolMessage message, out DeviceId? deviceId)` uses the same factory selection as discovery, but stops after identification.

| Operation | `TryIdentify` | Incoming discovery message |
| --- | --- | --- |
| Select a factory and determine `DeviceId` | Yes | Yes |
| Create and initialize a new device | No | When the ID is new |
| Call `UpdateDevice` for an existing device | No | Yes |
| Refresh the last-seen timestamp | No | Yes |
| Add a device to `Devices` | No | When a new device is created |

On success, the nullable annotation guarantees a non-null output. On no match, the method returns `false` and sets the output to `null`. A null input is rejected. Factory exceptions are not converted to an unidentified result by the explorer; callers that need a resilient inspection boundary should handle them there.

Factories should keep `TryIdentify` limited to identity extraction. The explorer does not call `CreateDevice` or `UpdateDevice` during identity-only lookup, but it cannot prevent side effects inside a factory's identification implementation.

This method is useful for packet inspectors and filters: traffic can be classified without creating live devices or preventing inactive devices from timing out. An application-level device manager can delegate to the explorer rather than maintain a second registry of factories.

## Discovery And Initialization

```mermaid
flowchart TD
    Message["Incoming RX message"] --> Match{"Matching factory and ID?"}
    Match -- no --> Ignore["Ignore for discovery"]
    Match -- yes --> Seen["Refresh last seen"]
    Seen --> Existing{"Device already exists?"}
    Existing -- yes --> Update["UpdateDevice"]
    Existing -- no --> Create["CreateDevice and Initialize"]
    Create --> Devices["Devices"]
    Devices --> Complete["State becomes Complete"]
    Complete --> Initialized["InitializedDevices"]
```

`Devices` includes discovered devices. `InitializedDevices` adds each device on its first `Complete` notification and removes it when the device is removed from `Devices`; it does not continuously filter the current initialization or link state. Initialization and link connectivity are distinct concepts, so inspect the relevant state when readiness matters.

The timeout timer removes inactive devices according to `ClientDeviceBrowserConfig.DeviceTimeoutMs` and `DeviceCheckIntervalMs`. Elapsed time and timer scheduling use the explorer's `TimeProvider`. Configure it with `IDeviceExplorerBuilder.SetTimeProvider`; the builder defaults independently to `TimeProvider.System`, even if the connection uses another provider. `TryIdentify` does not refresh last-seen timestamps. Dispose the explorer to stop discovery subscriptions, its timer, and its owned devices and clear both device collections; dispose the connection through its own owner.

`ClientDevice<TDeviceId>.Initialize()` starts initialization without returning an awaitable completion. Repeated calls are ignored after initialization has started successfully; they do not create another microservice generation. An initialization failure in the asynchronous attempt sets `Failed` and schedules another attempt through `Context.TimeProvider` after `ClientDeviceConfig.RequestDelayAfterFailMs`. Observe `State` to track completion.

## Requests And Periodic Sending

`MicroserviceBase<TBaseMessage>` request helpers subscribe for a matching response before sending. Each attempt owns a timeout source using `Context.TimeProvider` and links it to caller cancellation and the microservice's disposal token. The subscription, linked source, and timeout source are disposed when the attempt finishes, including an early response.

`InternalCall` uses `timeoutMs` per attempt and `attemptCount` as the total number of attempts. Exhausted response timeouts produce `TimeoutException`; caller cancellation and microservice disposal stop retries. The public connection `SendAndWaitAnswer` timeout overloads accept their own optional `TimeProvider`, defaulting to `TimeProvider.System`. These helpers do not automatically link response waiting to connection disposal; supply a caller lifetime token for that scope.

`WaitUntilConnect` waits for link connectivity. `WaitUntilConnectAndInit` then waits for initialization with a new timeout scope: its `timeoutMs` applies separately to each phase, rather than as one total deadline. Both helpers use the supplied provider and release their timeout sources and subscriptions on completion.

`ProtocolMessageTransponder<TMessage>` uses its supplied provider for periodic sends. Starting it again replaces its timer. If a previous send is still running, the next tick is skipped instead of queued. `Set` serializes message updates with sending, so it can block until an active send finishes; do not call it from the per-send callback. `Stop` prevents future ticks but does not cancel a send already in progress. Disposal cancels the active send; first-call `DisposeAsync` also awaits its completion before releasing resources, while `Dispose` defers that cleanup.

## Identity Is Optional Per Message

A registered factory may recognize some message kinds and reject others. Keep an explicit unidentified category when a message has no device identity. Do not assign the most recently seen device on an endpoint to every subsequent message: multiple devices can share a transport connection.

The shared layer has no mandatory system/component fields or protocol-specific addressing. A device factory supplies the meaning of `DeviceId` for its protocol.

## Source Files And Tests

- `src/Asv.IO/Devices/Explorer/IDeviceExplorer.cs` and `DeviceExplorer.cs`.
- `src/Asv.IO/Devices/Explorer/Factories/IClientDeviceFactory.cs`.
- `src/Asv.IO/Devices/Client/IClientDevice.cs` and `DeviceId.cs`.
- `src/Asv.IO/Devices/Client/ClientDevice.cs`, `Devices/MicroserviceBase.cs`, and `Protocol/ProtocolHelper.cs`.
- `src/Asv.IO/Protocol/Tools/ProtocolMessageTransponder.cs`.
- `src/Asv.IO.Test/Devices/Explorer/DeviceExplorerTest.cs`, including `TryIdentify_KnownMessage_DoesNotCreateDevice`, `TryIdentify_FactoriesRegisteredOutOfOrder_UsesFirstMatchingPriority`, and `TryIdentify_ExistingDevice_DoesNotRefreshLastSeen`.
- `src/Asv.IO.Test/Devices/DeviceLifetimeRegressionTest.cs`, `ProtocolRequestLifetimeTest.cs`, and `Protocol/Tools/ProtocolMessageTransponderLifetimeTest.cs`.

See [Connections](IO-Connections.md) for transport identity and [Fields and visitors](IO-Visitors.md) for protocol-independent field inspection.
