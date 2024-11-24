using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Ports;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Asv.IO;

public class SerialProtocolEndpoint(
    SerialPort port,
    string id,
    ProtocolEndpointConfig config,
    ImmutableArray<IProtocolParser> parsers,
    ImmutableArray<IProtocolFeature> features,
    ChannelWriter<IProtocolMessage> rxChannel, 
    ChannelWriter<ProtocolException> errorChannel,
    IProtocolContext context,
    IStatisticHandler statisticHandler)
    : ProtocolEndpoint(id, config, parsers, features,rxChannel,errorChannel,context)
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