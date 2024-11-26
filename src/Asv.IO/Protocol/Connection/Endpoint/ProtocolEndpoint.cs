using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;

namespace Asv.IO;


public abstract class ProtocolEndpoint: ProtocolConnection, IProtocolEndpoint
{
    private readonly TimeSpan _readEmptyLoopDelay;
    private readonly ImmutableArray<IProtocolParser> _parsers;
    private readonly Channel<IProtocolMessage> _txChannel;
    private readonly Channel<IProtocolMessage> _rxChannel;
    private readonly ILogger<ProtocolEndpoint> _logger;
    private readonly IDisposable _parserSub;
    private readonly ReactiveProperty<ProtocolConnectionException?> _lastError = new (null);
    private readonly ImmutableHashSet<string> _parserAvailable;

    protected ProtocolEndpoint(
        string id, 
        ProtocolPortConfig config, 
        ImmutableArray<IProtocolParser> parsers, 
        IProtocolContext context,
        IStatisticHandler? statistic = null)
        :base(id, context,statistic)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(context);
        _readEmptyLoopDelay = TimeSpan.FromMilliseconds(config.ReadEmptyLoopDelayMs);
        _logger = context.LoggerFactory.CreateLogger<ProtocolEndpoint>();
        _parsers = parsers;
        _parserAvailable = _parsers.Select(x=>x.Info.Id).ToImmutableHashSet();
        _txChannel = config.TxQueueSize <= 0 
            ? Channel.CreateUnbounded<IProtocolMessage>() 
            : Channel.CreateBounded<IProtocolMessage>(new BoundedChannelOptions(config.TxQueueSize)
            {
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = false,
                FullMode = config.DropMessageWhenFullTxQueue ? BoundedChannelFullMode.DropOldest: BoundedChannelFullMode.Wait
            }, TxMessageDropped);
        
        _rxChannel = config.RxQueueSize <= 0 
            ? Channel.CreateUnbounded<IProtocolMessage>() 
            : Channel.CreateBounded<IProtocolMessage>(new BoundedChannelOptions(config.RxQueueSize)
            {
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = true,
                FullMode = config.DropMessageWhenFullRxQueue ? BoundedChannelFullMode.DropOldest: BoundedChannelFullMode.Wait
            }, RxMessageDropped);
        var disposableBuilder = Disposable.CreateBuilder();
        foreach (var parser in _parsers)
        {
            parser.OnMessage
                .SubscribeAwait((x,cancel) => _rxChannel.Writer.WriteAsync(x,cancel))
                .AddTo(ref disposableBuilder);
            
            /*parser.OnMessage
                .Subscribe(InternalPublishRxMessage)
                .AddTo(ref disposableBuilder);*/
        }
        _parserSub = disposableBuilder.Build();
        Task.Factory.StartNew(ReadLoop, TaskCreationOptions.LongRunning, DisposeCancel);
        Task.Factory.StartNew(WriteLoop, TaskCreationOptions.LongRunning, DisposeCancel);
        Task.Factory.StartNew(PublishRxLoop, TaskCreationOptions.LongRunning, DisposeCancel);
    }

    private void RxMessageDropped(IProtocolMessage droppedMessage)
    {
        StatisticHandler.IncrementDropRxMessage();
        _logger.ZLogWarning($"Dropped message (rx queue is full) {droppedMessage.Protocol.Id} {droppedMessage.Name} ");
    }
    private void TxMessageDropped(IProtocolMessage droppedMessage)
    {
        StatisticHandler.IncrementDropTxMessage();
        _logger.ZLogWarning($"Dropped message (tx queue is full) {droppedMessage.Protocol.Id} {droppedMessage.Name} "); 
    }
    private async void PublishRxLoop(object? obj)
    {
        try
        {
            while (IsDisposed == false)
            {
                try
                {
                    var message = await _rxChannel.Reader.ReadAsync(DisposeCancel);
                    StatisticHandler.IncrementRxMessage();
                    InternalPublishRxMessage(message);
                }
                catch (ProtocolConnectionException e)
                {
                    _logger.ZLogError(e, $"Error in '{nameof(PublishRxLoop)}':{e.Message}");
                    StatisticHandler.IncrementRxError();
                    InternalPublishRxError(e);
                }
                catch (Exception e)
                {
                    _logger.ZLogError(e, $"Error in '{nameof(PublishRxLoop)}':{e.Message}");
                    StatisticHandler.IncrementRxError();
                    InternalPublishRxError(new ProtocolConnectionException(this,$"Error in '{nameof(PublishRxLoop)}': {e.Message}",e));
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error in publish loop");
            StatisticHandler.IncrementRxError();
            InternalPublishRxError(new ProtocolConnectionException(this,$"Error in '{nameof(PublishRxLoop)}': {e.Message}",e));
            Debug.Assert(false);
            Debugger.Break();
        }
    }

    private async void ReadLoop(object? o)
    {
        try
        {
            while (IsDisposed == false)
            {
                var bufferSize = GetAvailableBytesToRead();
                if (bufferSize == 0)
                {
                    // if no bytes received - wait some time
                    await Task.Delay(_readEmptyLoopDelay, Context.TimeProvider, DisposeCancel);
                    continue;
                }

                using var mem = MemoryPool<byte>.Shared.Rent(bufferSize);
                var readBytes = await InternalRead(mem.Memory, DisposeCancel);
                StatisticHandler.AddRxBytes(readBytes);
                for (var i = 0; i < readBytes; i++)
                {
                    var b = mem.Memory.Span[i];
                    for (var j = 0; j < _parsers.Length; j++)
                    {
                        var parser = _parsers[j];
                        if (!parser.Push(b)) continue;
                        _parsers.ForEach(x=>x.Reset());
                        break;
                    }
                }
            }

        }
        catch (Exception e)
        {
            _lastError.OnNext(new ProtocolConnectionException(this, $"Error at read loop: {e.Message}",e));
            _logger.ZLogError(e, $"Error while reading loop {Id}");
            InternalPublishRxError(new ProtocolConnectionException(this, $"Error at read loop: {e.Message}",e));
        }
    }
    
    protected abstract int GetAvailableBytesToRead();
    protected abstract ValueTask<int> InternalRead(Memory<byte> memory, CancellationToken cancel);
    protected abstract ValueTask<int> InternalWrite(ReadOnlyMemory<byte> memory, CancellationToken cancel);
    private async void WriteLoop(object? o)
    {
        try
        {
            while (IsDisposed == false && _lastError.CurrentValue == null)
            {
                var msg = await _txChannel.Reader.ReadAsync(DisposeCancel);
                // skip not supported messages
                if (_parserAvailable.Contains(msg.Protocol.Id) == false) continue;
                var newMessage = await InternalFilterTxMessage(msg);
                if (newMessage == null) continue;
                var size = newMessage.GetByteSize();
                using var mem = MemoryPool<byte>.Shared.Rent(size);
                var writeBytes = await newMessage.Serialize(mem.Memory);
                var sendBytes = await InternalWrite(mem.Memory[..writeBytes], DisposeCancel);
                Debug.Assert(writeBytes == sendBytes);
                StatisticHandler.AddTxBytes(sendBytes);
                StatisticHandler.IncrementTxMessages();
            }
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, $"Error while writing loop {Id}");
            InternalPublishTxError(new ProtocolConnectionException(this, $"Error at write loop: {e.Message}",e));
            _lastError.OnNext(new ProtocolConnectionException(this, $"Error at write loop: {e.Message}",e));
        }
    }
  
    public override ValueTask Send(IProtocolMessage message, CancellationToken cancel = default)
    {
        if (IsDisposed) return ValueTask.CompletedTask;
        if (_lastError.CurrentValue != null) return ValueTask.CompletedTask;
        return _txChannel.Writer.WriteAsync(message, cancel);
    }

    public IEnumerable<IProtocolParser> Parsers => _parsers;
    public ReadOnlyReactiveProperty<ProtocolConnectionException?> LastError => _lastError;

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logger.ZLogTrace($"{nameof(Dispose)} {Id}");
            foreach (var parser in _parsers)
            {
                parser.Dispose();
            }
            _lastError.Dispose();
            _parserSub.Dispose();
        }
    }


    protected override async ValueTask DisposeAsyncCore()
    {
        _logger.ZLogTrace($"{nameof(DisposeAsync)} {Id}");
        await CastAndDispose(_lastError);
        await CastAndDispose(_parserSub);
        foreach (var parser in _parsers)
        {
            await CastAndDispose(parser);
        }
        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }

    public override string ToString()
    {
        return $"[ENDPOINT]({Id})";
    }

    #endregion
}