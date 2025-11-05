using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using RJCP.IO.Ports;

namespace Asv.IO;

public sealed class AlternativeSerialProtocolEndpoint : ProtocolEndpoint
{
    private readonly SerialPortStream _serial;
    private readonly bool _initializationComplete;

    public AlternativeSerialProtocolEndpoint(
        string id,
        SerialProtocolPortConfig config,
        ImmutableArray<IProtocolParser> parsers,
        IProtocolContext context,
        IStatisticHandler statisticHandler
    )
        : base(id, config, parsers, context, statisticHandler)
    {
        _serial = new SerialPortStream(
            config.PortName,
            config.BoundRate,
            config.DataBits,
            ConvertParity(config.Parity),
            ConvertStopBits(config.StopBits)
        )
        {
            WriteBufferSize = config.WriteBufferSize,
            WriteTimeout = config.WriteTimeout,
            ReadBufferSize = config.ReadBufferSize,
            ReadTimeout = config.ReadTimeout,
        };
        _serial.Open();
        _initializationComplete = true;
    }

    private static StopBits ConvertStopBits(System.IO.Ports.StopBits configStopBits)
    {
        return configStopBits switch
        {
            System.IO.Ports.StopBits.None => StopBits.One,
            System.IO.Ports.StopBits.One => StopBits.One,
            System.IO.Ports.StopBits.Two => StopBits.Two,
            System.IO.Ports.StopBits.OnePointFive => StopBits.One5,
            _ => throw new ArgumentOutOfRangeException(
                nameof(configStopBits),
                configStopBits,
                null
            ),
        };
    }

    private static Parity ConvertParity(System.IO.Ports.Parity configParity)
    {
        return configParity switch
        {
            System.IO.Ports.Parity.None => Parity.None,
            System.IO.Ports.Parity.Odd => Parity.Odd,
            System.IO.Ports.Parity.Even => Parity.Even,
            System.IO.Ports.Parity.Mark => Parity.Mark,
            System.IO.Ports.Parity.Space => Parity.Space,
            _ => throw new ArgumentOutOfRangeException(nameof(configParity), configParity, null),
        };
    }

    public SerialPortStream SerialPort => _serial;

    protected override int GetAvailableBytesToRead()
    {
        if (!_initializationComplete)
        {
            // this can happen when the read loop starts from ProtocolEndpoint ctor
            return 0;
        }
        if (!_serial.IsOpen)
        {
            throw new InvalidOperationException("Serial port is not open.");
        }
        return _serial.BytesToRead;
    }

    protected override ValueTask<int> InternalRead(Memory<byte> memory, CancellationToken cancel)
    {
        return _serial.ReadAsync(memory, cancel);
    }

    protected override async ValueTask<int> InternalWrite(
        ReadOnlyMemory<byte> memory,
        CancellationToken cancel
    )
    {
        await _serial.WriteAsync(memory, cancel);
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

        await _serial.DisposeAsync();
        await base.DisposeAsyncCore();
    }
    #endregion
}
