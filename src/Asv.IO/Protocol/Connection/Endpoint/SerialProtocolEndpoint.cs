using System;
using System.Collections.Immutable;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public sealed class SerialProtocolEndpoint : ProtocolEndpoint
{
    private readonly SerialPort _serial;

    public SerialProtocolEndpoint(
        string id,
        SerialProtocolPortConfig config,
        ImmutableArray<IProtocolParser> parsers,
        IProtocolContext context,
        IStatisticHandler statisticHandler)
        : base(id, config, parsers, context, statisticHandler)
    {
        _serial = new SerialPort(
            config.PortName,
            config.BoundRate,
            config.Parity,
            config.DataBits,
            config.StopBits
        )
        {
            WriteBufferSize = config.WriteBufferSize,
            WriteTimeout = config.WriteTimeout,
            ReadBufferSize = config.ReadBufferSize,
            ReadTimeout = config.ReadTimeout,
        };
        _serial.Open();
    }

    public SerialPort SerialPort => _serial;

    protected override int GetAvailableBytesToRead()
    {
        if (!_serial.IsOpen)
        {
            throw new InvalidOperationException("Serial port is not open.");
        }
        return _serial.BytesToRead;
    }

    protected override ValueTask<int> InternalRead(Memory<byte> memory, CancellationToken cancel)
    {
        return _serial.BaseStream.ReadAsync(memory, cancel);
    }

    protected override async ValueTask<int> InternalWrite(
        ReadOnlyMemory<byte> memory,
        CancellationToken cancel
    )
    {
        await _serial.BaseStream.WriteAsync(memory, cancel);
        return memory.Length;
    }

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_serial.IsOpen)
            {
                _serial.Close();
            }

            _serial.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_serial.IsOpen)
        {
            _serial.Close();
        }

        _serial.Dispose();
        await base.DisposeAsyncCore();
    }
    #endregion
}
