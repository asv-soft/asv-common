# Connections

`IProtocolConnection` is the common message-oriented contract for routers, ports, and endpoints. It exposes RX/TX messages, RX/TX errors, statistics, tags, formatting, and `Send`.

## Topology

```mermaid
flowchart TD
    Router["IProtocolRouter"] --> PortA["IProtocolPort A"]
    Router --> PortB["IProtocolPort B"]
    PortA --> EndpointA["IProtocolEndpoint A1"]
    PortA --> EndpointB["IProtocolEndpoint A2"]
    PortB --> EndpointC["IProtocolEndpoint B1"]
    EndpointA --> Parsers["Protocol parsers"]
    EndpointA --> Encoders["Outbound encoders, one per protocol"]
```

| Level | Responsibility | Topology notifications |
| --- | --- | --- |
| Router | Aggregates ports and sends through its current port set. | `PortAdded`, `PortRemoved`, `PortUpdated` |
| Port | Configures a transport, manages endpoints, and exposes enabled/status/error state. | `EndpointAdded`, `EndpointRemoved` |
| Endpoint | Performs transport I/O, parsing, and outbound encoding for one connection. | Owned by the port; exposes parsers, `LastError`, and disposal state. |

`IProtocolConnection.DisplayName` is a compact display label; `Description` provides
transport details for tooltips. Neither property replaces `Id`, and display names
need not be unique. Existing custom implementations fall back to `Id` by default.

Built-in ports prefer a nonblank `Config.Name`. Otherwise TCP/UDP ports show the
transport and address (including IPv6 brackets), and serial ports show the device
basename, for example `Serial · ttyUSB0`. Port descriptions expand the connection
settings into labeled lines and retain the connection URI as the final line.

TCP/UDP endpoints show the remote address; their descriptions include local and
remote addresses and the original endpoint ID. Socket addresses are captured at
construction so metadata remains readable after disconnection or disposal. Serial
endpoints show the device basename and full serial settings; virtual endpoints
retain their ID as a label. Serial endpoint descriptions keep compact framing
notation such as `115200 · 8N1`; their format is separate from port descriptions.

A server port can have several endpoints. One endpoint can carry messages from several logical devices. Keep the pair of port ID and endpoint ID when recording an observation; do not infer device identity from endpoint identity alone.

## Transport Selection

`router.AddPort(...)` selects a registered transport factory by URI scheme.

| Scheme | Transport |
| --- | --- |
| `tcp` | TCP client |
| `tcps` | TCP server; this scheme does not mean TLS. |
| `udp` | UDP |
| `serial` | Serial port |

`ProtocolPortConfig` exposes common options such as `Name`, `IsEnabled`, and enabled protocols. Concrete transport configurations add their own options. Read the relevant configuration class before constructing a URI with additional options.

## Port Descriptions

Built-in port `Description` values use multiline English labels and invariant
numeric formatting. The transport and its primary address or serial settings are
always included. Reconnect interval, TX/RX queue capacities, and enabled protocol
names describe the effective settings, including defaults. The protocol list
reflects the factories selected for the port.

For example, a TCP client configured with
`tcp://192.168.1.10:5760?reconnect=3000&tx_queue=200` shows a reconnect interval of
3 seconds and an RX capacity inherited from TX. With no protocol factories
selected, its description is:

```text
Transport: TCP client
Remote address: 192.168.1.10:5760
Protocols: None
Reconnect interval: 3 s
TX queue: 200 messages
RX queue: 200 messages (inherited from TX)
Connection string: tcp://192.168.1.10:5760/?reconnect=3000&tx_queue=200
```

| Transport | Transport-specific lines |
| --- | --- |
| TCP client | `Remote address` with host and port. |
| TCP server | `Listen address`; `Pending connection queue` when `maxConnection` is explicitly configured. This value is the `Socket.Listen` backlog, not a limit on active clients. |
| UDP | `Local address` and sorted, distinct `Allowed remote endpoints`. Without configured peers: `Remote endpoints: discovered from incoming packets`. Resolved allowed endpoints are cached; reading `Description` does not perform DNS lookups. |
| Serial | Full `Device` path, `Baud rate` in bit/s, `Data bits`, full `Parity` name, and numeric `Stop bits`, for example `1.5`. |

Without queue or reconnect options, physical ports show the default 5-second
reconnect interval and 100-message TX/RX capacities. Capacities at or below zero
are displayed as `Unbounded`. An inherited RX capacity is labeled explicitly;
an explicit `rx_queue` is displayed independently. A reconnect interval of `-1`
is displayed as `Disabled`, meaning no timed retry.

For serial ports with `version=2`, the alternative serial implementation maps
`stop=None` to one stop bit. The description makes that effective setting explicit:
`Stop bits: 1 (configured as None)`.

Optional lines appear only when the corresponding option is configured: name,
configured enabled state, version, queue overflow policies, and empty-read delay.
Serial ports also show explicitly configured read/write buffer sizes and timeouts.
`Enabled in configuration` describes the configuration value rather than a live
connection status. An enabled queue drop policy is displayed as
`Drop oldest messages`; a policy on an unbounded queue is labeled
`Not applicable (unbounded queue)`. See [Queueing](#queueing) for the meaning of the
queue settings.

The last line is always `Connection string: ...`, using the current
`Config.ToString()` URI. It retains all configured options but may normalize the
input URI spelling or escaping. Descriptions remain readable after disposal.

`VirtualPort` implements an endpoint rather than a configurable physical port.
It uses the same labeled style without physical transport options:

```text
Transport: Virtual
Endpoint: simulation
Connection string: virtual://simulation
```

## RX And TX Are Level-Specific

```mermaid
sequenceDiagram
    participant Application
    participant Router
    participant Port
    participant Endpoint
    participant Transport
    Application->>Router: Send(message)
    Router->>Port: Send(message)
    Port->>Endpoint: Queue message
    Router-->>Application: Router OnTxMessage
    Endpoint->>Endpoint: Apply TX filters, encode once
    Endpoint->>Transport: Write actual encoded bytes
    Endpoint-->>Application: Endpoint OnTxMessage
```

The diagram shows different observation stages, not a guaranteed scheduling order between the router callback and the endpoint write loop. Router TX indicates completion of router-level sending to ports. A physical endpoint publishes TX after its write operation; it is still not a remote acknowledgement.

RX aggregation flows from endpoint to port to router. Physical endpoints await an internal forwarding handler at each built-in connection level, including asynchronous RX features, so a slow parent can apply backpressure to the endpoint reader. Virtual ports and custom connections that use synchronous publication follow the fallback policy described below. Public `OnRxMessage` observations remain available at each level. Observing every level without recording the level produces duplicate-looking observations of the same message.

Error streams are also level-specific. Routers and ports forward RX messages, but do not aggregate their children's `OnRxError` or `OnTxError` streams. Observe an endpoint directly for its transport errors. A port watches endpoint `LastError` to remove the failed endpoint and, where configured by the port implementation, reconnect.

## Observe A Known Endpoint

```csharp
using Asv.IO;
using R3;

static IDisposable ObserveEndpoint(IProtocolEndpoint endpoint)
{
    return endpoint.OnRxMessage.Subscribe(message =>
    {
        Console.WriteLine($"RX {endpoint.Id}: {message.Name}");
    });
}
```

For a topology-wide monitor, maintain one subscription scope per port and per endpoint. Subscribe to topology changes and reconcile the current collections without adding duplicate subscriptions. Dispose endpoint subscriptions immediately on removal; dispose all remaining scopes when the monitor stops.

## Queueing

Physical endpoints use separate receive and transmit channels:

| Option | Behavior |
| --- | --- |
| `rx_queue` / `RxQueueSize` | Receive capacity; defaults to 100. |
| `tx_queue` / `TxQueueSize` | Transmit capacity; defaults to 100. |
| `rx_drop` / `DropMessageWhenFullRxQueue` | When `true`, discard the oldest queued RX message to accept the newest. Defaults to `false`, which waits for space. |
| `tx_drop` / `DropMessageWhenFullTxQueue` | The corresponding TX policy. |
| Queue capacity at or below zero | Explicitly selects an unbounded channel. |

An explicit `rx_queue` is independent of `tx_queue`. For compatibility, an existing URI that specifies only `tx_queue` continues to use that value for both capacities. Setting `RxQueueSize` writes the separate `rx_queue` option.

Parser `Push` and message emission are synchronous. With the RX wait policy, the read loop holds at most one parsed message awaiting channel space before supplying another input byte. In addition to the configured queue, one message can be in publication and one in that handoff. A parser that emits another message while the handoff is occupied fails the endpoint instead of creating an unbounded backlog.

The protected synchronous `InternalPublishRxMessage` entry point remains available to existing derived connections and is used by the virtual port. It processes one publication at a time with a FIFO of up to 100 waiting messages. If that queue is full, the incoming message is dropped and the RX drop statistic increases. This fallback cannot pause its synchronous producer; its drop-newest policy is separate from the physical endpoint channel settings.

For a physical endpoint, `Send` waits only for admission to the TX queue. Its caller's cancellation token governs that wait; cancelling it after enqueueing does not withdraw the message or cancel the later write. The write loop uses the endpoint's lifetime token.

Routers and ports await `Send` on each child in their current collection. Completion does not establish successful delivery: a filter can reject the message, an aggregate can have no children, and a disposed or failed physical endpoint returns without enqueueing. A physical endpoint can also skip a queued message whose protocol has no parser enabled, or discard it under the configured drop policy. Endpoint `OnTxMessage` indicates a completed local write; a remote acknowledgement requires a protocol-level response.

The physical endpoint's single-reader transmit loop applies TX filters before selecting an encoder and encoding the resulting message. Messages dropped from the queue or rejected by a filter do not advance encoder state. Each endpoint retains its own registered encoder per protocol, so sending the same message through multiple endpoints keeps each stateful outbound stream independent. Protocols without a custom encoder use the shared stateless `DefaultProtocolEncoder.Instance`. Routers and ports forward message objects without sharing a stateful encoder between endpoints.

Only the actual encoded bytes are passed to the transport. TCP endpoints submit the complete frame in one `Socket.SendAsync` call. TX byte and message statistics advance after a complete write; bytes from an incomplete write are not added to those counters. If a transport reports an incomplete write or fails, the endpoint reports the error, completes its channels, and cancels its loops. It does not roll back encoding or automatically resend a possibly partial frame. A replacement endpoint starts with fresh encoder instances.

Virtual ports also honor the actual encoded length. A semaphore serializes filtering, encoding, and publication to the virtual byte stream, keeping encoder state changes in the same order as the emitted frames. If encoding or byte publication throws, `Send` reports the exception through TX errors and `LastError` without rethrowing it; subsequent sends return without publishing another frame. Filter exceptions occur outside this error handler and propagate to the caller.

Direct `IDataStream` and `Stream` encoding helpers have no endpoint queue. Their caller owns synchronization and must serialize the complete encode-and-send operation when sharing a stateful encoder. See [Serialization](IO-Serialization.md) for explicit helpers and buffer lifetime requirements.

## Startup And Disposal

Built-in serial, TCP, and UDP endpoints initialize their transport state before starting read, write, and RX publication loops. Custom subclasses can use the protected `ProtocolEndpoint` constructor overload with `startImmediately: false`, initialize their fields, and call `Start()` at the end of construction. The existing constructor retains immediate startup for compatibility.

Endpoint disposal cancels transport operations and queue waits immediately. Synchronous `Dispose()` starts tracked cleanup and returns without waiting for all loops. The endpoint returns temporary buffers, disposes its parser subscriptions and owned parsers, releases queued messages, and clears encoder references after its loops exit. `DisposeAsync()` waits for this cleanup when it is the first disposal call. Disposal is once-only: calling `DisposeAsync()` after `Dispose()` does not provide a second wait for completion.

Asynchronous RX and TX feature waits also end when their connection is disposed. Features should still honor their supplied cancellation token: stopping the wait cannot terminate a feature's own work if it ignores cancellation. Virtual ports serialize parser access and defer parser disposal until an active synchronous read exits; a callback can call `Dispose()` without returning a parser's buffer while `Push` is still using it.

Removing a port through the router disposes that port. A monitor observes these lifetimes; it must not dispose shared ports or endpoints just because an inspection page closes. Messages are mutable objects, so copy values needed for history instead of retaining a supposedly frozen live reference.

## Transport-Specific Lifetimes

UDP endpoints ignore empty datagrams. Cancellation or disposal releases the shared receive producer if it is waiting for an endpoint to consume a datagram. A failed serial endpoint construction releases the parsers it was given.

The legacy `TcpServerPort` byte stream has a per-recipient send FIFO of 100 frames, shared by forwarded data and explicit sends. Queued frames own a byte copy so the receive buffer can be reused. Forwarding does not wait for a slow peer; a full forwarding queue disconnects that peer. When the server is connected, an explicit `Send` awaits the queued writes to its current clients and returns a Boolean result through `PortBase`; with no clients, it can return `true` without sending any bytes. A graceful remote close removes and disposes the client. Asynchronous server disposal waits for the tracked receive and accept loops and the writers of clients captured when the server stops.

Disposing a text or Telnet wrapper cancels the token passed to an active send; the underlying `IDataStream` must honor that token for the send to finish promptly. JSON requests link their cancellation token to wrapper disposal, while standalone JSON `Send` and `SendText` use only the caller's token. Telnet exposes received text through `OnReceive`, and completion of a text stream ends the response wait in the `ITextStream.RequestText` helper. Text and Telnet wrappers release their subscriptions without disposing the underlying byte stream. A JSON wrapper disposes its text stream only when constructed with `disposeStream: true`.

## Source Files

- `src/Asv.IO/Protocol/Connection/IProtocolConnection.cs`.
- `src/Asv.IO/Protocol/Connection/Router/ProtocolRouter.cs`.
- `src/Asv.IO/Protocol/Connection/Port/ProtocolPort.cs`.
- `src/Asv.IO/Protocol/Connection/Endpoint/ProtocolEndpoint.cs`.
- `src/Asv.IO/Protocol/Connection/Virtual/VirtualPort.cs`.
- `src/Asv.IO/Streams/Ports/Tcp/TcpServerPort.cs`.

See [Devices](IO-Devices.md) for logical identity and [Features and formatters](IO-Features.md) for processing stages.
