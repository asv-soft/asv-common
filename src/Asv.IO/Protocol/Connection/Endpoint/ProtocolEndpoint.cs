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

public class ProtocolEndpointConfig
{
    public int OutputQueueSize { get; set; } = 100;
    public int ReadEmptyLoopDelayMs { get; set; } = 30;
    public bool DropMessageWhenFullTxQueue { get; set; } = false;

    public override string ToString()
    {
        return $"{nameof(OutputQueueSize)}: {OutputQueueSize}, {nameof(ReadEmptyLoopDelayMs)}: {ReadEmptyLoopDelayMs} {nameof(DropMessageWhenFullTxQueue)}: {DropMessageWhenFullTxQueue}";
    }
}
public abstract class ProtocolEndpoint: ProtocolConnection, IProtocolEndpoint
{
    private readonly TimeSpan _readEmptyLoopDelay;
    private readonly ImmutableArray<IProtocolParser> _parsers;
    private readonly Channel<IProtocolMessage> _outputChannel;
    private readonly ILogger<ProtocolEndpoint> _logger;
    private readonly IDisposable _parserSub;
    private readonly ReactiveProperty<bool> _isConnected = new (true);
    private readonly ImmutableHashSet<string> _parserAvailable;

    protected ProtocolEndpoint(
        string id, 
        ProtocolEndpointConfig config, 
        ImmutableArray<IProtocolParser> parsers, 
        ImmutableArray<IProtocolFeature> features,
        ChannelWriter<IProtocolMessage> rxChannel, 
        ChannelWriter<ProtocolException> errorChannel,
        IProtocolCore core,
        IStatisticHandler? statistic = null)
        :base(id, features, rxChannel, errorChannel,core,statistic)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(core);
        _readEmptyLoopDelay = TimeSpan.FromMilliseconds(config.ReadEmptyLoopDelayMs);
        _logger = core.LoggerFactory.CreateLogger<ProtocolEndpoint>();
        _parsers = parsers;
        _parserAvailable = _parsers.Select(x=>x.Info.Id).ToImmutableHashSet();
        var disposableBuilder = Disposable.CreateBuilder();
        foreach (var parser in _parsers)
        {
            parser.OnMessage
                .Subscribe(InternalPublishRxMessage)
                .AddTo(ref disposableBuilder);
        }
        _parserSub = disposableBuilder.Build();
        _outputChannel = config.OutputQueueSize <= 0 
            ? Channel.CreateUnbounded<IProtocolMessage>() 
            : Channel.CreateBounded<IProtocolMessage>(new BoundedChannelOptions(config.OutputQueueSize)
            {
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = false,
                FullMode = config.DropMessageWhenFullTxQueue ? BoundedChannelFullMode.DropOldest: BoundedChannelFullMode.Wait
            }, ItemDropped);
        
        var writeThread = new Thread(WriteLoop) { IsBackground = true, Name = $"{id} WriteLoop" };
        var readThread = new Thread(ReadLoop) { IsBackground = true, Name = $"{id} ReadLoop" };
        writeThread.Start();
        readThread.Start();
    }

    private void ItemDropped(IProtocolMessage droppedMessage)
    {
        _logger.ZLogTrace($"Dropped message (rx queue is full) {droppedMessage.Protocol.Id} {droppedMessage.Name} "); 
    }

    private async void ReadLoop()
    {
        try
        {
            while (DisposeCancel.IsCancellationRequested == false)
            {
                var bufferSize = GetAvailableBytesToRead();
                if (bufferSize == 0)
                {
                    // if no bytes received - wait some time
                    await Task.Delay(_readEmptyLoopDelay, Core.TimeProvider, DisposeCancel);
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
            _logger.ZLogError(e, $"Error while reading loop {Id}");
            InternalOnTxError(new ProtocolConnectionException(this, $"Error at read loop:{e.Message}",e));
            _isConnected.OnNext(false);
        }
    }
    
    protected abstract int GetAvailableBytesToRead();
    protected abstract ValueTask<int> InternalRead(Memory<byte> memory, CancellationToken cancel);
    protected abstract ValueTask<int> InternalWrite(ReadOnlyMemory<byte> memory, CancellationToken cancel);
    private async void WriteLoop()
    {
        try
        {
            while (DisposeCancel.IsCancellationRequested == false && _isConnected.CurrentValue)
            {
                var msg = await _outputChannel.Reader.ReadAsync(DisposeCancel);
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
            InternalOnTxError(new ProtocolConnectionException(this, $"Error at write loop:{e.Message}",e));
            _isConnected.OnNext(false);
        }
    }
  
    public override ValueTask Send(IProtocolMessage message, CancellationToken cancel = default)
    {
        if (IsDisposed) return ValueTask.CompletedTask;
        if (_isConnected.CurrentValue == false) return ValueTask.CompletedTask;
        return _outputChannel.Writer.WriteAsync(message, cancel);
    }

    public IEnumerable<IProtocolParser> Parsers => _parsers;
    public ReadOnlyReactiveProperty<bool> IsConnected => _isConnected;

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
            _isConnected.Dispose();
            _parserSub.Dispose();
        }
    }


    protected override async ValueTask DisposeAsyncCore()
    {
        _logger.ZLogTrace($"{nameof(DisposeAsync)} {Id}");
        await CastAndDispose(_isConnected);
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