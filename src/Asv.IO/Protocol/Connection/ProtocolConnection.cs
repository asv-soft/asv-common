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

public class ProtocolConnectionConfig
{
    public int OutputQueueSize { get; set; } = 100;
    public int ReadEmptyLoopDelayMs { get; set; } = 30;
}
public abstract class ProtocolConnection : IProtocolConnection
{
    private readonly IPipeCore _core;
    private uint _statBytesReceived;
    private uint _statBytesSent;
    private uint _statMessageSent;
    private uint _statMessageReceived;
    private readonly TimeSpan _readEmptyLoopDelay;
    private readonly ImmutableArray<IProtocolParser> _parsers;
    private readonly ImmutableArray<IProtocolRouteFilter> _filters;
    private readonly Subject<IProtocolMessage> _onMessageReceived = new();
    private readonly Subject<IProtocolMessage> _onMessageSent = new();
    private readonly Channel<IProtocolMessage> _outputChannel;
    private readonly CancellationTokenSource _disposeCancel;
    private readonly TagList _tags = [];
    private readonly ILogger<ProtocolConnection> _logger;
    private int _isDisposed;
    private readonly IDisposable _parserSub;

    protected ProtocolConnection(string id, ProtocolConnectionConfig config, IEnumerable<IProtocolParser> parsers, IEnumerable<IProtocolRouteFilter> filters, IPipeCore core)
    {
        _core = core;
        _readEmptyLoopDelay = TimeSpan.FromMilliseconds(config.ReadEmptyLoopDelayMs);
        _logger = core.LoggerFactory.CreateLogger<ProtocolConnection>();
        Id = id;
        _filters = [..filters.OrderBy(x=>x.Priority)];
        _parsers = [..parsers];
        
        var disposableBuilder = Disposable.CreateBuilder();
        foreach (var parser in _parsers)
        {
            parser.OnMessage.Subscribe(InternalOnMessageReceived).AddTo(ref disposableBuilder);
        }
        _parserSub = disposableBuilder.Build();
        _disposeCancel = new CancellationTokenSource();
        _outputChannel = config.OutputQueueSize <= 0 
            ? Channel.CreateUnbounded<IProtocolMessage>() 
            : Channel.CreateBounded<IProtocolMessage>(config.OutputQueueSize);
        
        var writeThread = new Thread(WriteLoop) { IsBackground = true, Name = $"{id} WriteLoop" };
        var readThread = new Thread(ReadLoop) { IsBackground = true, Name = $"{id} ReadLoop" };
        writeThread.Start();
        readThread.Start();
    }

    private void InternalOnMessageReceived(IProtocolMessage message)
    {
        Interlocked.Increment(ref _statMessageReceived);
        foreach (var filter in _filters)
        {
            if (filter.OnReceiveFilterAndTransform(ref message, this) == false) return;
        }
        _onMessageReceived.OnNext(message);
    }

    public string Id { get; }
    private async void ReadLoop()
    {
        try
        {
            while (_disposeCancel.IsCancellationRequested == false)
            {
                var bufferSize = GetAvailableBytesToRead();
                if (bufferSize == 0)
                {
                    // if no bytes received - wait some time
                    await Task.Delay(_readEmptyLoopDelay, _core.TimeProvider, _disposeCancel.Token);
                }

                using var mem = MemoryPool<byte>.Shared.Rent(bufferSize);
                var readBytes = await InternalRead(mem.Memory, _disposeCancel.Token);
                Interlocked.Add(ref _statBytesReceived, (uint)readBytes);
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
            _onMessageReceived.OnErrorResume(new ProtocolConnectionException(this, $"Error at read loop:{e.Message}",e));
            await DisposeAsync();
        }
    }

    protected abstract int GetAvailableBytesToRead();
    protected abstract ValueTask<int> InternalRead(Memory<byte> memory, CancellationToken cancel);
    protected abstract ValueTask<int> InternalWrite(ReadOnlyMemory<byte> memory, CancellationToken cancel);
    private async void WriteLoop()
    {
        try
        {
            while (_disposeCancel.IsCancellationRequested == false)
            {
                var msg = await _outputChannel.Reader.ReadAsync(_disposeCancel.Token);
                foreach (var filter in _filters)
                {
                    if (filter.OnSendFilterTransform(ref msg, this) == false) return;
                }
                using var mem = MemoryPool<byte>.Shared.Rent(msg.GetByteSize());
                var writeBytes = await msg.Serialize(mem.Memory);
                var sendBytes = await InternalWrite(mem.Memory[..writeBytes], _disposeCancel.Token);
                Debug.Assert(writeBytes == sendBytes);
                Interlocked.Add(ref _statBytesSent, (uint)writeBytes);
                Interlocked.Increment(ref _statMessageSent);
                _onMessageSent.OnNext(msg);
            }
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, $"Error while writing loop {Id}");
            _onMessageSent.OnErrorResume(new ProtocolConnectionException(this, $"Error at write loop:{e.Message}",e));
            await DisposeAsync();
        }
    }
    public uint StatRxBytes => _statBytesReceived;
    public uint StatTxBytes => _statBytesSent;
    public uint StatTxMessages => _statMessageSent;
    public uint StatRxMessages => _statMessageReceived;
    public TagList Tags => _tags;
    public IEnumerable<IProtocolParser> Parsers => _parsers;
    public Observable<IProtocolMessage> OnMessageReceived => _onMessageReceived;
    public Observable<IProtocolMessage> OnMessageSent => _onMessageSent;
    public ValueTask Send(IProtocolMessage message, CancellationToken cancel = default)
    {
        return IsDisposed ? ValueTask.CompletedTask : _outputChannel.Writer.WriteAsync(message, cancel);
    }

    #region Dispose

    public bool IsDisposed => _isDisposed != 0;
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var parser in _parsers)
            {
                parser.Dispose();
            }
            _parserSub.Dispose();
            _onMessageReceived.Dispose();
            _onMessageSent.Dispose();
            _disposeCancel.Dispose();
        }
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
        {
            _logger.ZLogTrace($"Skip duplicate {nameof(Dispose)} {Id}");
            return;
        }
        _logger.ZLogTrace($"{nameof(Dispose)} {Id}");
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        await CastAndDispose(_parserSub);
        await CastAndDispose(_onMessageReceived);
        await CastAndDispose(_onMessageSent);
        await CastAndDispose(_disposeCancel);
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

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
        {
            _logger.ZLogTrace($"Skip duplicate {nameof(DisposeAsync)} {Id}");
            return;
        }
        _logger.ZLogTrace($"{nameof(DisposeAsync)} {Id}");
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    #endregion
}