using System;
using System.Collections.Immutable;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public sealed class SerialProtocolEndpoint(
    SerialPort port,
    string id,
    ProtocolPortConfig config,
    ImmutableArray<IProtocolParser> parsers,
    IProtocolContext context,
    IStatisticHandler statisticHandler
) : ProtocolEndpoint(id, config, parsers, context, statisticHandler)
{
    SerialPort SerialPort => port;

    protected override int GetAvailableBytesToRead()
    {
        if (!port.IsOpen)
        {
            throw new InvalidOperationException("Serial port is not open.");
        }
        return port.BytesToRead;
    }

    protected override ValueTask<int> InternalRead(Memory<byte> memory, CancellationToken cancel)
    {
        return port.BaseStream.ReadAsync(memory, cancel);
    }

    protected override async ValueTask<int> InternalWrite(
        ReadOnlyMemory<byte> memory,
        CancellationToken cancel
    )
    {
        await port.BaseStream.WriteAsync(memory, cancel);
        return memory.Length;
    }

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (port.IsOpen)
            {
                port.Close();
            }

            port.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (port.IsOpen)
        {
            port.Close();
        }

        port.Dispose();
        await base.DisposeAsyncCore();
    }
    #endregion
}
