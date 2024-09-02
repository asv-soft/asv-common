using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.IO.Pipelines;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using StreamPipeReaderOptions = System.IO.Pipelines.StreamPipeReaderOptions;

namespace Asv.IO;


// Own static logger manager
public static class LogManager
{
    static ILogger _globalLogger = default!;
    static ILoggerFactory _loggerFactory = default!;

    public static void SetLoggerFactory(ILoggerFactory loggerFactory, string categoryName)
    {
        LogManager._loggerFactory = loggerFactory;
        LogManager._globalLogger = loggerFactory.CreateLogger(categoryName);
    }

    public static ILogger Logger => _globalLogger;

    // standard LoggerFactory caches logger per category so no need to cache in this manager
    public static ILogger<T> GetLogger<T>() where T : class => _loggerFactory.CreateLogger<T>();
    public static ILogger GetLogger(string categoryName) => _loggerFactory.CreateLogger(categoryName);
}


public interface IPipePort
{
    IRxValue<bool> IsEnabled { get; }
    IRxValue<PortState> State { get; }
    
}

public class PipeSerialPort : IPipePort
{
    private readonly SerialPortConfig _config;
    private SerialPort _serialPort;
    private readonly RxValue<IDuplexPipe?> _dataPipe;

    public PipeSerialPort(SerialPortConfig config)
    {
        _config = config;
        Pipe a = new Pipe();
        _dataPipe = new RxValue<IDuplexPipe?>(default);
    }

    public void Enable()
    {
        _serialPort = new SerialPort(_config.PortName, _config.BoundRate, _config.Parity, _config.DataBits, _config.StopBits)
        {
            WriteTimeout = _config.WriteTimeout,
            WriteBufferSize = _config.WriteBufferSize
        };
        _serialPort.Open();
        
        _dataPipe.OnNext(new DuplexPipe(_serialPort.BaseStream));
    }

    public IRxValue<bool> IsEnabled { get; }
    
    public PipeReader Input { get; }
    public PipeWriter Output { get; }
}

public class PipeConnection:DisposableOnceWithCancel
{
    private readonly IDuplexPipe _pipe;
    private readonly Thread _thread;

    public PipeConnection(IDuplexPipe pipe)
    {
        _pipe = pipe;
        _thread = new Thread(ProcessLoop);
        _thread.Start();
    }

    private async void ProcessLoop(object? obj)
    {
        while (IsDisposed == false)
        {
             var result = await _pipe.Input.ReadAsync(DisposeCancel);
             foreach (var b in result.Buffer)
             {
                 
             }
             if (result.IsCompleted) return;
             
        }
    }
}



public class PipeDecoder
{
    public async Task ProcessMessagesAsync(PipeReader reader, CancellationToken cancellationToken = default)
    {
        try
        {
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken);
                var buffer = result.Buffer;
                
                try
                {
                    // Process all messages from the buffer, modifying the input buffer on each
                    // iteration.
                    while (TryParseLines(ref buffer, out Message message))
                    {
                        await ProcessMessageAsync(message);
                    }

                    // There's no more data to be processed.
                    if (result.IsCompleted)
                    {
                        if (buffer.Length > 0)
                        {
                            // The message is incomplete and there's no more data to process.
                            throw new InvalidDataException("Incomplete message.");
                        }
                        break;
                    }
                }
                finally
                {
                    // Since all messages in the buffer are being processed, you can use the
                    // remaining buffer's Start and End position to determine consumed and examined.
                    reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
        }
        finally
        {
            await reader.CompleteAsync();
        }
    }

    private bool TryParseLines(ref ReadOnlySequence<byte> buffer, out Message message)
    {
        
    }
}