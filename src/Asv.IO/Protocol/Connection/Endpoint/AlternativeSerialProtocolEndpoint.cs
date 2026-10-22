using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using RJCP.IO.Ports;

namespace Asv.IO;

public sealed class AlternativeSerialProtocolEndpoint : ProtocolEndpoint
{
    private readonly SerialProtocolPortConfig _displayConfig;
    private readonly SerialPortStream? _serial;

    public AlternativeSerialProtocolEndpoint(
        string id,
        SerialProtocolPortConfig config,
        ImmutableArray<IProtocolParser> parsers,
        IProtocolContext context,
        IStatisticHandler statisticHandler
    )
        : base(id, config, parsers, context, statisticHandler, startImmediately: false)
    {
        _displayConfig = config;
        try
        {
            _serial = new SerialPortStream(
                config.PortName,
                config.BoundRate,
                config.DataBits,
                ConvertParity(config.Parity),
                ConvertStopBits(config.StopBits)
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

    public override string DisplayName => _displayConfig is null
        ? Id
        : ProtocolConnectionDisplay.SerialName(_displayConfig);
    public override string Description => _displayConfig is null
        ? Id
        : ProtocolConnectionDisplay.SerialDescription(_displayConfig);

    public SerialPortStream SerialPort =>
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
        return SerialPort.ReadAsync(memory, cancel);
    }

    protected override async ValueTask<int> InternalWrite(
        ReadOnlyMemory<byte> memory,
        CancellationToken cancel
    )
    {
        await SerialPort.WriteAsync(memory, cancel).ConfigureAwait(false);
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

        if (_serial != null)
        {
            await _serial.DisposeAsync().ConfigureAwait(false);
        }
        await base.DisposeAsyncCore().ConfigureAwait(false);
    }
    #endregion
}
