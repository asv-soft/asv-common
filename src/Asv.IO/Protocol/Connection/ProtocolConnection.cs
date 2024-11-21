using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;

namespace Asv.IO;

public abstract class ProtocolConnection : AsyncDisposableWithCancel, IProtocolConnection, ISupportTag
{
    private readonly ImmutableArray<IProtocolFeature> _features;
    private readonly Subject<IProtocolMessage> _internalRxMessage = new();
    private readonly Subject<IProtocolMessage> _internalTxMessage = new();
    private ProtocolTags _internalTags = [];
    private uint _statRxBytes;
    private uint _statTxBytes;
    private uint _statRxMessages;
    private uint _statTxMessages;
    private uint _statRxError;
    private uint _statTxError;
    private readonly ILogger<ProtocolConnection> _logger;

    protected ProtocolConnection(string id, ImmutableArray<IProtocolFeature> features, IProtocolCore core)
    {
        _features = features;
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(core);
        Id = id;
        Core = core;
        _logger = core.LoggerFactory.CreateLogger<ProtocolConnection>();
        foreach (var feature in _features)
        {
            feature.Register(this);
        }
    }

    public string Id { get; }
    protected IProtocolCore Core { get; }
    
    public uint StatRxBytes => _statRxBytes;
    public uint StatTxBytes => _statTxBytes;
    public uint StatRxMessages => _statRxMessages;
    public uint StatTxMessages => _statTxMessages;
    public uint RxError => _statRxError;
    public uint TxError => _statTxError;

    public Observable<IProtocolMessage> OnRxMessage => _internalRxMessage;
    public Observable<IProtocolMessage> OnTxMessage => _internalTxMessage;
    public abstract ValueTask Send(IProtocolMessage message, CancellationToken cancel = default);
    
    protected async ValueTask<IProtocolMessage?> InternalFilterTxMessage(IProtocolMessage message)
    {
        foreach (var item in _features)
        {
            var newMsg = await item.ProcessTx(message, this, DisposeCancel);
            if (newMsg == null) return null;
            message = newMsg;
        }
        return message;
    }
    protected async void InternalPublishRxMessage(IProtocolMessage message)
    {
        try
        {
            foreach (var item in _features)
            {
                var newMsg = await item.ProcessRx(message, this, DisposeCancel);
                if (newMsg == null) return;
                message = newMsg;
            }
            Interlocked.Increment(ref _statRxMessages);
            Interlocked.Add(ref _statRxBytes, (uint)message.GetByteSize());
            _internalRxMessage.OnNext(message);
        }
        catch (Exception ex)
        {
            InternalPublishRxError(ex);
        }
    }
    protected void InternalPublishRxError(Exception ex)
    {
        Interlocked.Increment(ref _statRxError);
        _internalRxMessage.OnErrorResume(ex);
    }
    protected void InternalPublishTxMessage(IProtocolMessage message)
    {
        Interlocked.Increment(ref _statTxMessages);
        Interlocked.Add(ref _statTxBytes, (uint)message.GetByteSize());
        
        _internalRxMessage.OnNext(message);
    }
    protected void InternalOnTxError(Exception ex)
    {
        Interlocked.Increment(ref _statTxError);
        _internalTxMessage.OnErrorResume(ex);
    }
    public ref ProtocolTags Tags => ref _internalTags;
    
    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logger.ZLogTrace($"Dispose port {this}");
            foreach (var feature in _features)
            {
                feature.Unregister(this);
            }
            _internalRxMessage.Dispose();
            _internalTxMessage.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        foreach (var feature in _features)
        {
            feature.Unregister(this);
        }
        await CastAndDispose(_internalRxMessage);
        await CastAndDispose(_internalTxMessage);

        await base.DisposeAsyncCore();

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }

    #endregion

    
}