using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public class SerialProtocolConnection(
    SerialPort port,
    string id,
    ProtocolConnectionConfig config,
    IEnumerable<IProtocolParser> parsers,
    IEnumerable<IProtocolRouteFilter> filters,
    IPipeCore core)
    : ProtocolConnection(id, config, parsers, filters, core)
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