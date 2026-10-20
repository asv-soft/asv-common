using System;
using System.Collections.Immutable;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public sealed class SerialProtocolEndpoint : ProtocolEndpoint
{
    private readonly SerialPort? _serial;

    public SerialProtocolEndpoint(
        string id,
        SerialProtocolPortConfig config,
        ImmutableArray<IProtocolParser> parsers,
        IProtocolContext context,
        IStatisticHandler statisticHandler
    )
        : base(id, config, parsers, context, statisticHandler, startImmediately: false)
    {
        try
        {
            _serial = new SerialPort(
                config.PortName,
                config.BoundRate,
                config.Parity,
                config.DataBits,
                config.StopBits
            );
            _serial.WriteBufferSize = config.WriteBufferSize;
            _serial.WriteTimeout = config.WriteTimeout;
            _serial.ReadBufferSize = config.ReadBufferSize;
            _serial.ReadTimeout = config.ReadTimeout;
            _serial.Open();
            Start();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public SerialPort SerialPort =>
        _serial ?? throw new InvalidOperationException("Serial port is not initialized.");

    protected override int GetAvailableBytesToRead()
    {
        if (_serial is not { IsOpen: true })
        {
            throw new InvalidOperationException("Serial port is not open.");
        }
        return _serial.BytesToRead;
    }

    protected override ValueTask<int> InternalRead(Memory<byte> memory, CancellationToken cancel)
    {
        return SerialPort.BaseStream.ReadAsync(memory, cancel);
    }

    protected override async ValueTask<int> InternalWrite(
        ReadOnlyMemory<byte> memory,
        CancellationToken cancel
    )
    {
        await SerialPort.BaseStream.WriteAsync(memory, cancel);
        return memory.Length;
    }

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CancelPendingOperations();
            if (_serial is { IsOpen: true })
            {
                _serial.Close();
            }

            _serial?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        CancelPendingOperations();
        if (_serial is { IsOpen: true })
        {
            _serial.Close();
        }

        _serial?.Dispose();
        await base.DisposeAsyncCore();
    }
    #endregion
}
