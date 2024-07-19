using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO.Pipelines;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZLogger;

namespace Asv.IO;

public interface IProtocolConnection:IDisposable
{
    string Name { get; }
    long TxBytes { get; }
    long TxMessages { get; }
    long RxBytes { get; }
    long RxMessages { get; }
    IObservable<ProtocolException> OnError { get; }
    IObservable<IProtocolMessage> OnRxMessage { get; }
    IObservable<IProtocolMessage> OnTxMessage { get; }
    Task Send(IProtocolMessage message, CancellationToken cancel = default);
}

public class ProtocolConnection : DisposableOnceWithCancel, IProtocolConnection
{
    #region Constants
    
    private const int DefaultDelayAfterErrorMs = 1000;
    
    #endregion
    
    #region Statictic

    private long _txBytes;
    private long _txMessages;
    private long _rxBytes;
    private long _rxMessage;

    #endregion
    
    #region Metrics

    private static readonly Meter Meter = new Meter("asv-io-connection");
    private static readonly Counter<int> RxBytesCounter = Meter.CreateCounter<int>("rx_bytes","bytes","Receive bytes");
    private static Histogram<int> _rxTick = Meter.CreateHistogram<int>("rx_tick","ms","");
    
    #endregion
    
    private readonly IDuplexPipe _pipe;
    private readonly ILogger<ProtocolConnection> _logger;
    private readonly ImmutableArray<IProtocolDecoder> _decoders;
    private readonly Subject<IProtocolMessage> _onTxMessage;
    private readonly Subject<ProtocolException> _onError;
    private long _decodeErrors;

    public ProtocolConnection(string name, IDuplexPipe pipe, IEnumerable<IProtocolDecoder> decoders,ILogger<ProtocolConnection>? logger = null, IScheduler? publishScheduler = null, bool disposeStream = false)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _pipe = pipe;
        _logger = logger ?? new NullLogger<ProtocolConnection>();
        if (disposeStream)
        {
            (_pipe as IDisposable)?.DisposeItWith(Disposable);
        }
        var onRxMessage = new Subject<IProtocolMessage>().DisposeItWith(Disposable);
        OnRxMessage = publishScheduler != null ? onRxMessage.ObserveOn(publishScheduler) : onRxMessage;
        
        _onTxMessage = new Subject<IProtocolMessage>().DisposeItWith(Disposable);
        OnTxMessage = publishScheduler !=null ? _onTxMessage.ObserveOn(publishScheduler) : _onTxMessage;
        
        _onError = new Subject<ProtocolException>().DisposeItWith(Disposable);
        OnError = publishScheduler != null ? _onError.ObserveOn(publishScheduler) : _onError;
        
        _decoders = decoders.ToImmutableArray();
        foreach (var decoder in _decoders)
        {
            decoder.DisposeItWith(Disposable);
            decoder.OnError.Subscribe(_onError).DisposeItWith(Disposable);
            decoder.OnRxMessage.Do(_=>Interlocked.Increment(ref _rxMessage)).Subscribe(onRxMessage).DisposeItWith(Disposable);
        }

        var thread = new Thread(ProcessingLoop);
        thread.Start();
    }

    #region Statistics
    public string Name { get; }
    public long TxBytes => Interlocked.Read(ref _txBytes);
    public long TxMessages => Interlocked.Read(ref _txMessages);
    public long RxBytes => Interlocked.Read(ref _rxBytes);
    public long RxMessages => Interlocked.Read(ref _rxMessage);
    
    #endregion
    private async void ProcessingLoop(object? obj)
    {
.3
    +
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    +...3333333333333333333333333333333333333333333333333333+..................................................+++++++++++++++++++++
    while (IsDisposed == false)
        {
            var reader = _pipe.Input;
            try
            {
                var result = await reader.ReadAsync(DisposeCancel);
                foreach (var buffer in result.Buffer)
                {
                    ProcessSegment(buffer.Span);
                }
                reader.AdvanceTo(result.Buffer.Start,result.Buffer.End);
                if (result.IsCompleted) return;
                if (result.IsCanceled) return;
            }
            catch (Exception e)
            {
                _logger.ZLogError(e,$"Error occured at '{Name}' connection processing loop");
                await Task.Delay(DefaultDelayAfterErrorMs);
            }
            
        }
    }

    

    private void ProcessSegment(ReadOnlySpan<byte> buffer)
    {
        if (IsDisposed) return;
        Interlocked.Add(ref _rxBytes, buffer.Length);
        RxBytesCounter.Add(buffer.Length);
        foreach (var data in buffer)
        {
            try
            {
                var packetFound = Enumerable.Any(_decoders, decoder => decoder.Read(data));
                if (!packetFound) continue;
                foreach (var decoder in _decoders)
                {
                    decoder.Reset();
                }
            }
            catch (Exception e)
            {
                Interlocked.Increment(ref _decodeErrors);
                _onError.OnNext(new ProtocolException("Decoder error",e));
                Debug.Assert(false);
            }
        }
    }

    public IObservable<ProtocolException> OnError { get; }
    public IObservable<IProtocolMessage> OnRxMessage { get; }
    public IObservable<IProtocolMessage> OnTxMessage {get;}

    public Task Send(IProtocolMessage message, CancellationToken cancel = default)
    {
        if (IsDisposed) return Task.CompletedTask;
        return Task.Run(() =>
        {
            if (IsDisposed) return;
            message.Serialize(_pipe.Output, out var txBytes);
            Interlocked.Increment(ref _txMessages);
            Interlocked.Add(ref _txBytes, txBytes);
            _onTxMessage.OnNext(message);
            
        }, cancel);

    }

    
}

