using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public class SerialProtocolEndpoint(
    SerialPort port,
    string id,
    ProtocolEndpointConfig config,
    ImmutableArray<IProtocolParser> parsers,
    ImmutableArray<IProtocolFeature> features,
    IProtocolCore core)
    : ProtocolEndpoint(id, config, parsers, features, core)
{
    protected override int GetAvailableBytesToRead() => port.BytesToRead;

    protected override ValueTask<int> InternalRead(Memory<byte> memory, CancellationToken cancel)
    {
        return port.BaseStream.ReadAsync(memory, cancel);
    }

    protected override async ValueTask<int> InternalWrite(ReadOnlyMemory<byte> memory, CancellationToken cancel)
    {
        await port.BaseStream.WriteAsync(memory, cancel);
        return memory.Length;
    }
}