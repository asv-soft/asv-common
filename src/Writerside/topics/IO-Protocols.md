# Protocols

The protocol layer constructs connections, decoders, and outbound encoders from an explicit configuration. `Protocol.Create(...)` produces an `IProtocolFactory`; that factory can create routers, standalone parsers, and virtual connections.

## Core Contracts

```mermaid
flowchart TD
    Configure["Protocol.Create(configure)"] --> Builder["IProtocolBuilder"]
    Builder --> Protocols["Protocols: parser and encoder factories"]
    Builder --> Ports["PortTypes: transport factories"]
    Builder --> Features["Features: message processing"]
    Builder --> Formatters["Formatters: text representation"]
    Builder --> Factory["IProtocolFactory"]
    Factory --> Router["CreateRouter(id)"]
    Factory --> Parser["CreateParser(protocolId)"]
    Factory --> Virtual["CreateVirtualConnection()"]
```

| Contract | Responsibility |
| --- | --- |
| `IProtocolBuilder` | Registers protocol parsers, optional encoders, port types, features, formatters, logging, metrics, and time services. |
| `IProtocolFactory` | Exposes available registrations and creates configured protocol objects. |
| `IProtocolContext` | Supplies shared configuration to connection and parser implementations. |
| `IProtocolParser` | Consumes bytes through `Push`, publishes decoded messages and parsing errors, and supports `Reset`. |
| `IProtocolEncoder` | Encodes messages for one outbound stream, reporting a capacity bound and the actual number of bytes written. |
| `IProtocolEncoderBuilder` | Adds optional encoder registration to a protocol parser builder. |
| `IProtocolEncoderContext` | Exposes optional encoder factories keyed by protocol ID. |
| `ProtocolInfo` | Carries a protocol ID and display name. |

The current builder registers the standard transport types and simple formatter by default. It does not select an application protocol automatically. `ClearAll()` clears the registrations and resets logging, metrics, and time configuration; register everything needed after calling it.

## Configuration

```csharp
using Asv.IO;

var factory = Protocol.Create(builder =>
{
    builder.Protocols.RegisterExampleProtocol();
    builder.Formatters.RegisterJsonFormatter();
    builder.Features.RegisterEndpointIdTagFeature();
});

await using var router = factory.CreateRouter("application");
foreach (var protocol in router.AvailableProtocols)
{
    Console.WriteLine($"{protocol.Id}: {protocol.Name}");
}
```

Production protocol packages provide their own registration extensions. The application selects those packages; the shared transport and inspection code can continue to use `IProtocolMessage`.

## Outbound Encoders

Existing protocols need no new registration. `DefaultProtocolEncoder` delegates to the message's `GetByteSize()` and `Serialize(ref Span<byte>)`, then reports the consumed span length. It serializes the message once and preserves the message's existing format, including serializers that consume fewer bytes than their reserved capacity.

A protocol that needs outbound stream state can register an encoder alongside its parser. For example, given the package's `ProtocolInfo` and encoder implementation:

```csharp
builder.Protocols.RegisterEncoder(info, context => new MyProtocolEncoder());
```

The factory must return a fresh stateful encoder on each call. Each endpoint creates and retains its own encoder for each protocol it sends. Receive parsers have separate state. Registering an encoder does not register a parser or add a protocol to `AvailableProtocols`; the usual parser registration remains necessary. `builder.Protocols.Clear()` clears both parser and encoder registrations. `ClearAll()` also performs this reset.

`IProtocolMessage`, `IProtocolBuilder`, and `IProtocolContext` keep their existing members. Encoder registration is an optional capability: `RegisterEncoder` requires a builder implementing `IProtocolEncoderBuilder` and throws `NotSupportedException` otherwise. A context without `IProtocolEncoderContext`, or without a factory for the requested protocol ID, uses `DefaultProtocolEncoder`. Existing custom builders and contexts therefore continue to support their existing protocols without implementing the new interfaces.

For a manually owned stream, `context.CreateEncoder(protocolId)` creates the appropriate encoder. Retain that instance for the stream lifetime; calling it for every message would lose stream state. See [Serialization](IO-Serialization.md) for the encoder contract and explicit encoding helpers.

## Parsing

A parser receives individual bytes. `OnMessage` publishes a recognized message; `OnError` publishes parsing failures. `Push` returns a boolean recognition result rather than the decoded object.

```mermaid
sequenceDiagram
    participant Transport
    participant Endpoint
    participant Parser
    participant Subscriber
    Transport->>Endpoint: Received bytes
    loop For each byte
        Endpoint->>Parser: Push(byte)
    end
    Parser->>Endpoint: OnMessage(message)
    Endpoint->>Subscriber: OnRxMessage(message), after processing
```

An endpoint can run several registered parsers over incoming data. In the current endpoint read loop, a successful parser result resets the parser set. Do not assume that every parser sees one independently framed stream or that every input byte produces a message.

## Ownership And Diagnostics

Dispose standalone parsers, routers, and virtual connections when their owner finishes. A router owns its ports; ports own their endpoints. Keep subscriber lifetimes explicit as well.

The current `VirtualConnection` forwards stream completion between its two sides. Disposing it can report an `ObjectDisposedException` through R3's unhandled-exception handler when the opposite subject has already been disposed. This cleanup limitation is separate from message decoding; do not interpret a successful round trip as proof that disposal is free of diagnostics.

Use the configured `TimeProvider`, logger factory, and meter factory when implementing protocol components. This keeps timing and diagnostics consistent and permits deterministic tests.

## Source Files

- `src/Asv.IO/Protocol/Protocol.cs` and `IProtocolBuilder.cs`.
- `src/Asv.IO/Protocol/Parser/IProtocolParser.cs` and `ProtocolParser.cs`.
- `src/Asv.IO/Protocol/Encoder/IProtocolEncoder.cs`, `ProtocolEncoderHelper.cs`, and `DefaultProtocolEncoder.cs`.
- `src/Asv.IO/Example/Protocol/ExampleProtocol.cs`.

See [Connections](IO-Connections.md) for transport composition and [Features and formatters](IO-Features.md) for message processing.
