using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;

namespace Asv.IO;

public abstract class ProtocolConnection : AsyncDisposableWithCancel, IProtocolConnection
{
    private readonly ImmutableArray<IProtocolFeature> _features;
    private readonly ChannelWriter<IProtocolMessage> _rxChannel;
    private readonly ChannelWriter<ProtocolException> _errorChannel;
    private ProtocolTags _internalTags = [];
    private readonly ILogger<ProtocolConnection> _logger;

    protected ProtocolConnection(string id, ImmutableArray<IProtocolFeature> features,
        ChannelWriter<IProtocolMessage> rxChannel, ChannelWriter<ProtocolException> errorChannel, IProtocolCore core, IStatisticHandler? statistic = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(rxChannel);
        ArgumentNullException.ThrowIfNull(errorChannel);
        ArgumentNullException.ThrowIfNull(core);
        _logger = core.LoggerFactory.CreateLogger<ProtocolConnection>();
        _features = features;
        _rxChannel = rxChannel;
        _errorChannel = errorChannel;
        if (statistic == null)
        {
            var value = new Statistic();
            Statistic = value;
            StatisticHandler = value;
        }
        else
        {
            var value = new InheritedStatistic(statistic);
            Statistic = value;
            StatisticHandler = value;
        }
        Id = id;
        Core = core;
        foreach (var feature in _features)
        {
            feature.Register(this);
        }
    }

    public string Id { get; }
    public IStatistic Statistic { get; }
    protected IStatisticHandler StatisticHandler { get; }
    protected IProtocolCore Core { get; }
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

            StatisticHandler.IncrementRxMessage();
            await _rxChannel.WriteAsync(message);
        }
        catch (ProtocolException ex)
        {
            InternalPublishRxError(ex);
        }
        catch (Exception ex)
        {
            InternalPublishRxError(new ProtocolException($"Error at publish rx message:{ex.Message}", ex));
        }
    }
   
    protected void InternalPublishRxError(ProtocolException ex)
    {
        StatisticHandler.IncrementRxError();
        _errorChannel.TryWrite(ex);
    }
   
    protected void InternalOnTxError(ProtocolException ex)
    {
        StatisticHandler.IncrementTxError();
        _errorChannel.TryWrite(ex);
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
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        foreach (var feature in _features)
        {
            feature.Unregister(this);
        }
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